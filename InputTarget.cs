using System.Windows.Automation;

namespace EnglishHelper;

// Only explicit clicks read text; focus tracking reads control metadata, never contents.
sealed class InputTarget
{
    public AutomationElement Element { get; }
    public IntPtr Window { get; }
    public string Original { get; }
    readonly int[] runtimeId;

    InputTarget(AutomationElement element, string original)
    {
        Element = element; Original = original;
        Window = Native.GetForegroundWindow(); runtimeId = element.GetRuntimeId();
    }

    public static bool IsEditable(AutomationElement? element)
    {
        if (element == null || element.Current.IsPassword || !element.Current.IsEnabled) return false;
        var type = element.Current.ControlType;
        if (type != ControlType.Edit && type != ControlType.Document) return false;
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var value)) return !((ValuePattern)value).Current.IsReadOnly;
        if (element.TryGetCurrentPattern(TextPattern.Pattern, out var text))
            return ((TextPattern)text).DocumentRange.GetAttributeValue(TextPattern.IsReadOnlyAttribute) is bool readOnly && !readOnly;
        return false;
    }

    public static InputTarget Capture()
    {
        var element = FocusedEditor();
        if (element == null || !IsEditable(element)) throw new Exception("请先点击要翻译的输入框。此软件若未开放输入框读取，请选中文字后使用托盘菜单查询。");
        var text = Read(element);
        if (string.IsNullOrWhiteSpace(text)) throw new Exception("输入框还是空的，先写一句话，再点气泡即可。");
        if (text.Length > 3000) throw new Exception("当前输入框超过 3,000 字符，请选中需要翻译的段落后使用查询按钮。");
        return new InputTarget(element, text);
    }

    public static AutomationElement? FocusedEditor()
    {
        var element = AutomationElement.FocusedElement;
        for (int depth = 0; element != null && depth < 5; depth++)
        {
            if (element.Current.IsPassword) return null;
            if (IsEditable(element)) return element;
            element = TreeWalker.ControlViewWalker.GetParent(element);
        }
        return null;
    }

    static string Read(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var value)) return ((ValuePattern)value).Current.Value;
        if (element.TryGetCurrentPattern(TextPattern.Pattern, out var text)) return ((TextPattern)text).DocumentRange.GetText(3001);
        throw new Exception("无法读取这个输入框。");
    }

    public bool IsStillFocused()
    {
        var focused = FocusedEditor();
        return Native.GetForegroundWindow() == Window && focused != null && focused.GetRuntimeId().SequenceEqual(runtimeId);
    }

    public async Task Replace(string translation)
    {
        if (!IsStillFocused()) throw new Exception("你已经切换了输入框，未替换文字。译文已保留，可复制使用。");
        if (!IsEditable(Element) || Read(Element) != Original) throw new Exception("翻译期间原文有变化，未替换文字。译文已保留，可复制使用。");
        // Select only the verified focused editor, never blindly select an entire page.
        Native.Ctrl(0x41);
        var selected = await Native.Selection();
        if (!IsStillFocused() || Normalize(selected) != Normalize(Original) || Read(Element) != Original)
            throw new Exception("无法确认输入框内容，已取消替换。译文已保留，可复制使用。");
        await Native.Paste(translation);
    }

    static string Normalize(string text) => text.Replace("\r\n", "\n").TrimEnd('\r', '\n');
}

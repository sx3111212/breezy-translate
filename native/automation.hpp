#pragma once
#include "common.hpp"
#include <uiautomation.h>

namespace breezy {
struct Anchor { RECT bounds{}; bool selection=false; bool editable=false; HWND foreground=nullptr; };
struct Target {
    Com<IUIAutomationElement> element;
    Com<IUIAutomationTextRange> selection;
    HWND foreground=nullptr;
    std::string original;
    bool writing=false;
    bool can_replace=false;
    HWND native_edit=nullptr;
    DWORD selection_start=0,selection_end=0;
};
class Automation {
    Com<IUIAutomation> api_;
    bool own_window(HWND window) const;
    bool password(IUIAutomationElement* element) const;
    Com<IUIAutomationElement> editor();
    std::string text(IUIAutomationElement* element) const;
public:
    Automation();
    bool editable(IUIAutomationElement* element) const;
    Anchor anchor(POINT released_at,bool follow_selection=true,bool follow_input=true);
    Target capture(bool writing,POINT released_at);
    void replace(const Target& target,const std::string& translated);
    bool same_focused(const Target& target);
};
struct Hotkey { UINT modifiers=0,key=0; bool valid=false; };
Hotkey parse_hotkey(const std::string& value);
std::string record_hotkey(UINT key);
class Hotkeys {
    HWND window_;
    Json applied_;
    bool suspended_=false;
    bool register_keys(const Json& preferences);
    void clear();
public:
    explicit Hotkeys(HWND window):window_(window){}
    ~Hotkeys(){clear();}
    bool apply(const Json& preferences);
    void suspend();
    void resume();
};
bool wait_modifiers();
void copy_text(HWND owner,const std::string& text);
std::string selected_via_clipboard(HWND owner);
void paste_text(HWND owner,const std::string& text);
}

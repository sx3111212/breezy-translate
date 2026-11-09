#include "automation.hpp"

namespace breezy {
template<class T>static Com<T> pattern(IUIAutomationElement* element,PATTERNID id){
    Com<T> result;if(!element)return result;Com<IUnknown> unknown;
    if(SUCCEEDED(element->GetCurrentPattern(id,unknown.put()))&&unknown)unknown->QueryInterface(__uuidof(T),(void**)result.put());return result;
}
static std::string bstr_text(BSTR value){if(!value)return {};auto result=utf8(std::wstring(value,SysStringLen(value)));SysFreeString(value);return result;}
static std::string range_text(IUIAutomationTextRange* range){BSTR value=nullptr;if(!range||FAILED(range->GetText(3001,&value)))return {};return bstr_text(value);}
static HWND native_edit(IUIAutomationElement* element){
    if(!element)return nullptr;UIA_HWND handle=nullptr;if(FAILED(element->get_CurrentNativeWindowHandle(&handle))||!handle)return nullptr;HWND window=(HWND)handle;wchar_t type[64]{};GetClassNameW(window,type,64);if(_wcsicmp(type,L"Edit")!=0)return nullptr;
    DWORD_PTR character=0;SendMessageTimeoutW(window,EM_GETPASSWORDCHAR,0,0,SMTO_ABORTIFHUNG,200,&character);if((GetWindowLongPtrW(window,GWL_STYLE)&ES_PASSWORD)||character)return nullptr;return window;
}
static std::string native_text(HWND window){
    DWORD_PTR length=0;if(!SendMessageTimeoutW(window,WM_GETTEXTLENGTH,0,0,SMTO_ABORTIFHUNG,500,&length)||length>1024*1024)throw std::runtime_error("Cannot safely read this input field.");std::wstring text((size_t)length+1,0);DWORD_PTR count=0;if(!SendMessageTimeoutW(window,WM_GETTEXT,text.size(),(LPARAM)text.data(),SMTO_ABORTIFHUNG,500,&count))throw std::runtime_error("Cannot read this input field.");text.resize((size_t)count);return utf8(text);
}
static bool native_selection(HWND window,DWORD& start,DWORD& end){DWORD_PTR result=0;return SendMessageTimeoutW(window,EM_GETSEL,(WPARAM)&start,(LPARAM)&end,SMTO_ABORTIFHUNG,200,&result)&&end>start;}
static bool native_replace(HWND window,DWORD start,DWORD end,const std::string& text){DWORD_PTR result=0;if(!SendMessageTimeoutW(window,EM_SETSEL,start,end,SMTO_ABORTIFHUNG,500,&result))return false;auto translated=wide(text);return SendMessageTimeoutW(window,EM_REPLACESEL,TRUE,(LPARAM)translated.c_str(),SMTO_ABORTIFHUNG,500,&result)!=0;}
Automation::Automation(){if(FAILED(CoCreateInstance(CLSID_CUIAutomation,nullptr,CLSCTX_INPROC_SERVER,__uuidof(IUIAutomation),(void**)api_.put())))throw std::runtime_error("Windows UI Automation is unavailable.");}
bool Automation::own_window(HWND window)const{DWORD process=0;GetWindowThreadProcessId(window,&process);return process==GetCurrentProcessId();}
bool Automation::password(IUIAutomationElement* element)const{BOOL value=TRUE;if(!element||FAILED(element->get_CurrentIsPassword(&value))||value)return true;UIA_HWND handle=nullptr;if(SUCCEEDED(element->get_CurrentNativeWindowHandle(&handle))&&handle){wchar_t type[64]{};GetClassNameW((HWND)handle,type,64);if(_wcsicmp(type,L"Edit")==0){DWORD_PTR character=0;SendMessageTimeoutW((HWND)handle,EM_GETPASSWORDCHAR,0,0,SMTO_ABORTIFHUNG,200,&character);if(character||(GetWindowLongPtrW((HWND)handle,GWL_STYLE)&ES_PASSWORD))return true;}}return false;}
bool Automation::editable(IUIAutomationElement* element)const{
    if(password(element))return false;BOOL enabled=FALSE;CONTROLTYPEID type=0;
    if(FAILED(element->get_CurrentIsEnabled(&enabled))||!enabled||FAILED(element->get_CurrentControlType(&type))||(type!=UIA_EditControlTypeId&&type!=UIA_DocumentControlTypeId))return false;
    if(HWND window=native_edit(element))return IsWindowEnabled(window)&&!(GetWindowLongPtrW(window,GWL_STYLE)&ES_READONLY);
    auto value=pattern<IUIAutomationValuePattern>(element,UIA_ValuePatternId);BOOL read_only=TRUE;
    if(value&&SUCCEEDED(value->get_CurrentIsReadOnly(&read_only)))return !read_only;
    auto text_pattern=pattern<IUIAutomationTextPattern>(element,UIA_TextPatternId);Com<IUIAutomationTextRange> range;
    if(text_pattern&&SUCCEEDED(text_pattern->get_DocumentRange(range.put()))&&range){VARIANT value;VariantInit(&value);HRESULT status=range->GetAttributeValue(UIA_IsReadOnlyAttributeId,&value);bool result=SUCCEEDED(status)&&value.vt==VT_BOOL&&value.boolVal==VARIANT_FALSE;VariantClear(&value);return result;}return false;
}
Com<IUIAutomationElement> Automation::editor(){
    Com<IUIAutomationElement> current;api_->GetFocusedElement(current.put());Com<IUIAutomationTreeWalker> walker;api_->get_ControlViewWalker(walker.put());
    for(int depth=0;current&&walker&&depth<5;depth++){
        if(password(current.get()))return {};
        if(editable(current.get()))return current;
        Com<IUIAutomationElement> parent;walker->GetParentElement(current.get(),parent.put());current=std::move(parent);
    }return {};
}
std::string Automation::text(IUIAutomationElement* element)const{
    if(HWND window=native_edit(element))return native_text(window);
    auto value=pattern<IUIAutomationValuePattern>(element,UIA_ValuePatternId);if(value){BSTR output=nullptr;if(SUCCEEDED(value->get_CurrentValue(&output)))return bstr_text(output);}
    auto text_pattern=pattern<IUIAutomationTextPattern>(element,UIA_TextPatternId);Com<IUIAutomationTextRange> range;
    if(text_pattern&&SUCCEEDED(text_pattern->get_DocumentRange(range.put())))return range_text(range.get());
    throw std::runtime_error("This application does not expose readable input text.");
}
Anchor Automation::anchor(POINT point,bool follow_selection,bool follow_input){
    Anchor result;result.foreground=GetForegroundWindow();if(!result.foreground||own_window(result.foreground))return result;
    DWORD process=0;GetWindowThreadProcessId(result.foreground,&process);
    std::vector<Com<IUIAutomationElement>> candidates;Com<IUIAutomationElement> under,focused;
    api_->ElementFromPoint(point,under.put());api_->GetFocusedElement(focused.put());if(under)candidates.push_back(under);if(focused)candidates.push_back(focused);
    Com<IUIAutomationTreeWalker> walker;api_->get_ControlViewWalker(walker.put());
    if(follow_selection)for(auto current:candidates)for(int depth=0;current&&walker&&depth<8;depth++){
        int owner=0;current->get_CurrentProcessId(&owner);if((DWORD)owner!=process||password(current.get()))break;
        if(HWND window=native_edit(current.get())){DWORD start=0,end=0;if(native_selection(window,start,end)){DWORD_PTR packed=0;RECT bounds{};GetWindowRect(window,&bounds);if(SendMessageTimeoutW(window,EM_POSFROMCHAR,start,0,SMTO_ABORTIFHUNG,200,&packed)&&packed!=(DWORD_PTR)-1){POINT position{(short)LOWORD(packed),(short)HIWORD(packed)};ClientToScreen(window,&position);bounds={position.x,position.y,position.x+40,position.y+22};}result.bounds=bounds;result.selection=GetForegroundWindow()==result.foreground;return result;}}
        auto pattern_=pattern<IUIAutomationTextPattern>(current.get(),UIA_TextPatternId);Com<IUIAutomationTextRangeArray> ranges;
        if(pattern_&&SUCCEEDED(pattern_->GetSelection(ranges.put()))&&ranges){int count=0;ranges->get_Length(&count);for(int i=0;i<count;i++){
            Com<IUIAutomationTextRange> range;ranges->GetElement(i,range.put());if(trim(range_text(range.get())).empty())continue;
            SAFEARRAY* rectangles=nullptr;
            if(SUCCEEDED(range->GetBoundingRectangles(&rectangles))&&rectangles){LONG low=0,high=-1;SafeArrayGetLBound(rectangles,1,&low);SafeArrayGetUBound(rectangles,1,&high);if(high-low>=3){double* values=nullptr;SafeArrayAccessData(rectangles,(void**)&values);result.bounds={(LONG)values[0],(LONG)values[1],(LONG)(values[0]+values[2]),(LONG)(values[1]+values[3])};SafeArrayUnaccessData(rectangles);}SafeArrayDestroy(rectangles);}
            if(result.bounds.right<=result.bounds.left)result.bounds={point.x-2,point.y-14,point.x+2,point.y+2};
            result.selection=GetForegroundWindow()==result.foreground;return result;
        }}
        Com<IUIAutomationElement> parent;walker->GetParentElement(current.get(),parent.put());current=std::move(parent);
    }
    if(follow_input){auto input=editor();if(input){input->get_CurrentBoundingRectangle(&result.bounds);result.editable=GetForegroundWindow()==result.foreground;}}
    return result;
}
Target Automation::capture(bool writing,POINT point){
    Target result;result.foreground=GetForegroundWindow();result.writing=writing;
    if(!result.foreground||own_window(result.foreground))throw std::runtime_error(writing?"Click the input field you want to translate first.":"noSelection");
    if(writing){result.element=editor();if(!result.element)throw std::runtime_error("Click a supported editable input field first.");result.original=text(result.element.get());}
    else{
        Com<IUIAutomationElement> current;api_->GetFocusedElement(current.put());Com<IUIAutomationTreeWalker> walker;api_->get_ControlViewWalker(walker.put());
        for(int depth=0;current&&walker&&depth<8;depth++){
            if(password(current.get()))throw std::runtime_error("Password fields are excluded.");
            if(HWND window=native_edit(current.get())){DWORD start=0,end=0;if(native_selection(window,start,end)){auto full=wide(native_text(window));if(end<=full.size()){result.element=current;result.native_edit=window;result.selection_start=start;result.selection_end=end;result.original=utf8(full.substr(start,end-start));break;}}}
            auto text_pattern=pattern<IUIAutomationTextPattern>(current.get(),UIA_TextPatternId);Com<IUIAutomationTextRangeArray> ranges;
            if(text_pattern&&SUCCEEDED(text_pattern->GetSelection(ranges.put()))&&ranges){int count=0;ranges->get_Length(&count);if(count){Com<IUIAutomationTextRange> range;ranges->GetElement(0,range.put());auto selected=range_text(range.get());if(!trim(selected).empty()){result.element=current;result.selection=range;result.original=selected;break;}}}
            Com<IUIAutomationElement> parent;walker->GetParentElement(current.get(),parent.put());current=std::move(parent);
        }
        // A point-based fallback covers applications where the selection is not the focused element.
        if(result.original.empty()){
            api_->ElementFromPoint(point,current.put());
            for(int depth=0;current&&walker&&depth<8;depth++){
                if(password(current.get()))throw std::runtime_error("Password fields are excluded.");
                if(HWND window=native_edit(current.get())){DWORD start=0,end=0;if(native_selection(window,start,end)){auto full=wide(native_text(window));if(end<=full.size()){result.element=current;result.native_edit=window;result.selection_start=start;result.selection_end=end;result.original=utf8(full.substr(start,end-start));break;}}}
                auto text_pattern=pattern<IUIAutomationTextPattern>(current.get(),UIA_TextPatternId);Com<IUIAutomationTextRangeArray> ranges;
                if(text_pattern&&SUCCEEDED(text_pattern->GetSelection(ranges.put()))&&ranges){int count=0;ranges->get_Length(&count);if(count){Com<IUIAutomationTextRange> range;ranges->GetElement(0,range.put());auto selected=range_text(range.get());if(!trim(selected).empty()){result.element=current;result.selection=range;result.original=selected;break;}}}
                Com<IUIAutomationElement> parent;walker->GetParentElement(current.get(),parent.put());current=std::move(parent);
            }
        }
    }
    if(trim(result.original).empty())throw std::runtime_error(writing?"The input field is empty.":"noSelection");
    result.can_replace=result.element&&editable(result.element.get());
    if(wide(result.original).size()>3000)throw std::runtime_error("Select no more than 3,000 characters.");
    if(GetForegroundWindow()!=result.foreground)throw std::runtime_error("The focused window changed. Please try again.");
    return result;
}
bool Automation::same_focused(const Target& target){
    if(GetForegroundWindow()!=target.foreground||!target.element||password(target.element.get()))return false;
    Com<IUIAutomationElement> focused=editor();BOOL same=FALSE;
    if(focused)api_->CompareElements(focused.get(),target.element.get(),&same);
    return same;
}
void Automation::replace(const Target& target,const std::string& translated){
    if(!target.element||!editable(target.element.get())||GetForegroundWindow()!=target.foreground)throw std::runtime_error("The original input is no longer focused. Copy the translation instead.");
    if(target.writing){
        if(!same_focused(target)||text(target.element.get())!=target.original)throw std::runtime_error("The input or focus changed. The translation was preserved without replacing text.");
        if(HWND window=native_edit(target.element.get())){if(!native_replace(window,0,(DWORD)wide(target.original).size(),translated))throw std::runtime_error("Cannot replace the original input.");return;}
        auto value=pattern<IUIAutomationValuePattern>(target.element.get(),UIA_ValuePatternId);
        if(value){auto string=wide(translated);BSTR argument=SysAllocStringLen(string.data(),(UINT)string.size());HRESULT status=value->SetValue(argument);SysFreeString(argument);if(SUCCEEDED(status))return;}
        auto text_pattern=pattern<IUIAutomationTextPattern>(target.element.get(),UIA_TextPatternId);Com<IUIAutomationTextRange> range;
        if(!text_pattern||FAILED(text_pattern->get_DocumentRange(range.put()))||FAILED(range->Select())||range_text(range.get())!=target.original)throw std::runtime_error("Cannot safely select the original input.");
    }else{
        if(target.native_edit){auto full=wide(native_text(target.native_edit));if(target.selection_end>full.size()||utf8(full.substr(target.selection_start,target.selection_end-target.selection_start))!=target.original)throw std::runtime_error("The original selection changed. Copy the translation instead.");if(!native_replace(target.native_edit,target.selection_start,target.selection_end,translated))throw std::runtime_error("Cannot replace the original selection.");return;}
        if(!target.selection||range_text(target.selection.get())!=target.original||FAILED(target.selection->Select()))throw std::runtime_error("The original selection changed. Copy the translation instead.");
    }
    if(GetForegroundWindow()!=target.foreground||password(target.element.get()))throw std::runtime_error("The focused window changed. Replacement was canceled.");
    paste_text(nullptr,translated);
}

static void chord(WORD key){INPUT input[4]{};for(auto& item:input)item.type=INPUT_KEYBOARD;input[0].ki.wVk=VK_CONTROL;input[1].ki.wVk=key;input[2].ki.wVk=key;input[2].ki.dwFlags=KEYEVENTF_KEYUP;input[3].ki.wVk=VK_CONTROL;input[3].ki.dwFlags=KEYEVENTF_KEYUP;if(SendInput(4,input,sizeof(INPUT))!=4)throw std::runtime_error("Windows blocked keyboard input for this application.");}
bool wait_modifiers(){for(int i=0;i<150;i++){if(!(GetAsyncKeyState(VK_CONTROL)&0x8000)&&!(GetAsyncKeyState(VK_MENU)&0x8000)&&!(GetAsyncKeyState(VK_SHIFT)&0x8000))return true;Sleep(10);}return false;}
void copy_text(HWND owner,const std::string& value){
    auto text=wide(value);if(!OpenClipboard(owner))throw std::runtime_error("The clipboard is busy. Please try again.");
    HGLOBAL memory=GlobalAlloc(GMEM_MOVEABLE,(text.size()+1)*sizeof(wchar_t));if(!memory){CloseClipboard();throw std::bad_alloc();}
    auto data=(wchar_t*)GlobalLock(memory);memcpy(data,text.c_str(),(text.size()+1)*sizeof(wchar_t));GlobalUnlock(memory);
    EmptyClipboard();if(!SetClipboardData(CF_UNICODETEXT,memory)){GlobalFree(memory);CloseClipboard();throw std::runtime_error("Cannot copy text.");}CloseClipboard();
}
struct ClipboardSnapshot{
    Com<IDataObject> previous;
    DWORD sequence=0;
    ClipboardSnapshot(){if(FAILED(OleGetClipboard(previous.put())))throw std::runtime_error("The clipboard is busy.");}
    ~ClipboardSnapshot(){if(sequence&&GetClipboardSequenceNumber()==sequence){if(SUCCEEDED(OleSetClipboard(previous.get()))&&previous)OleFlushClipboard();}}
};
template<class F>static auto clipboard_thread(F operation)->decltype(operation(HWND{})){
    return std::async(std::launch::async,[operation]{
        if(FAILED(OleInitialize(nullptr)))throw std::runtime_error("Cannot access the clipboard.");
        struct Apartment{~Apartment(){OleUninitialize();}} apartment;
        HWND owner=CreateWindowExW(0,L"STATIC",L"",0,0,0,0,0,HWND_MESSAGE,nullptr,GetModuleHandleW(nullptr),nullptr);
        struct Window{HWND value;~Window(){DestroyWindow(value);}} window{owner};
        return operation(owner);
    }).get();
}
std::string selected_via_clipboard(HWND){return clipboard_thread([](HWND owner){
    ClipboardSnapshot previous;DWORD before=GetClipboardSequenceNumber();chord('C');DWORD after=before;
    for(int i=0;i<40&&after==before;i++){Sleep(10);after=GetClipboardSequenceNumber();}
    if(after==before)return std::string();previous.sequence=after;std::string result;
    if(OpenClipboard(owner)){auto handle=GetClipboardData(CF_UNICODETEXT);auto text=handle?(wchar_t*)GlobalLock(handle):nullptr;if(text){result=utf8(text);GlobalUnlock(handle);}CloseClipboard();}return result;
});}
void paste_text(HWND,const std::string& text){clipboard_thread([text](HWND owner){ClipboardSnapshot previous;copy_text(owner,text);previous.sequence=GetClipboardSequenceNumber();chord('V');Sleep(180);});}
Hotkey parse_hotkey(const std::string& value){
    Hotkey result;std::istringstream stream(value);std::string part;
    while(std::getline(stream,part,'+')){
        part=lower(trim(part));if(part=="ctrl"||part=="control")result.modifiers|=MOD_CONTROL;else if(part=="alt")result.modifiers|=MOD_ALT;else if(part=="shift")result.modifiers|=MOD_SHIFT;
        else{if(result.key)return {};if(part.size()==1&&std::isalnum((unsigned char)part[0]))result.key=(UINT)std::toupper((unsigned char)part[0]);else if(part.size()==7&&part.rfind("numpad",0)==0&&part[6]>='0'&&part[6]<='9')result.key=VK_NUMPAD0+part[6]-'0';else if(part.size()>=2&&part[0]=='f'){try{size_t consumed=0;int number=std::stoi(part.substr(1),&consumed);if(consumed==part.size()-1&&number>=1&&number<=24)result.key=VK_F1+number-1;}catch(...){}}
        else if(part=="space")result.key=VK_SPACE;else if(part=="tab")result.key=VK_TAB;else if(part=="enter"||part=="return")result.key=VK_RETURN;else if(part=="home")result.key=VK_HOME;else if(part=="end")result.key=VK_END;else if(part=="pageup"||part=="prior")result.key=VK_PRIOR;else if(part=="pagedown"||part=="next")result.key=VK_NEXT;else if(part=="left")result.key=VK_LEFT;else if(part=="right")result.key=VK_RIGHT;else if(part=="up")result.key=VK_UP;else if(part=="down")result.key=VK_DOWN;else if(part=="delete")result.key=VK_DELETE;else if(part=="insert")result.key=VK_INSERT;else if(part=="multiply")result.key=VK_MULTIPLY;else if(part=="add")result.key=VK_ADD;else if(part=="separator")result.key=VK_SEPARATOR;else if(part=="subtract")result.key=VK_SUBTRACT;else if(part=="decimal")result.key=VK_DECIMAL;else if(part=="divide")result.key=VK_DIVIDE;}
    }
    result.valid=(result.modifiers&(MOD_CONTROL|MOD_ALT))&&result.key&&result.key!=VK_F12;return result;
}
std::string record_hotkey(UINT key){
    if(key==VK_CONTROL||key==VK_SHIFT||key==VK_MENU||key==VK_LWIN||key==VK_RWIN)return {};
    std::string result;if(GetKeyState(VK_CONTROL)&0x8000)result+="Ctrl+";if(GetKeyState(VK_MENU)&0x8000)result+="Alt+";if(GetKeyState(VK_SHIFT)&0x8000)result+="Shift+";
    if(key>=VK_F1&&key<=VK_F24)result+="F"+std::to_string(key-VK_F1+1);else if(key>=VK_NUMPAD0&&key<=VK_NUMPAD9)result+="NumPad"+std::to_string(key-VK_NUMPAD0);else if(key>='0'&&key<='9'||key>='A'&&key<='Z')result+=(char)key;else{static const std::map<UINT,std::string> names{{VK_SPACE,"Space"},{VK_TAB,"Tab"},{VK_RETURN,"Enter"},{VK_HOME,"Home"},{VK_END,"End"},{VK_PRIOR,"PageUp"},{VK_NEXT,"PageDown"},{VK_LEFT,"Left"},{VK_RIGHT,"Right"},{VK_UP,"Up"},{VK_DOWN,"Down"},{VK_DELETE,"Delete"},{VK_INSERT,"Insert"},{VK_MULTIPLY,"Multiply"},{VK_ADD,"Add"},{VK_SEPARATOR,"Separator"},{VK_SUBTRACT,"Subtract"},{VK_DECIMAL,"Decimal"},{VK_DIVIDE,"Divide"}};auto found=names.find(key);if(found==names.end())return {};result+=found->second;}return result;
}
void Hotkeys::clear(){UnregisterHotKey(window_,0x5101);UnregisterHotKey(window_,0x5102);}
bool Hotkeys::register_keys(const Json& value){
    if(!flag(value,"HotkeysEnabled",true))return true;auto read=parse_hotkey(get(value,"ReadHotkey")),write=parse_hotkey(get(value,"InputHotkey"));
    bool enable_read=flag(value,"ReadHotkeyEnabled",true),enable_write=flag(value,"InputHotkeyEnabled",true);
    if(enable_read&&!read.valid||enable_write&&!write.valid||enable_read&&enable_write&&read.modifiers==write.modifiers&&read.key==write.key)return false;
    if(enable_read&&!RegisterHotKey(window_,0x5101,read.modifiers|MOD_NOREPEAT,read.key))return false;
    if(enable_write&&!RegisterHotKey(window_,0x5102,write.modifiers|MOD_NOREPEAT,write.key)){clear();return false;}return true;
}
bool Hotkeys::apply(const Json& value){auto old=applied_;clear();if(!register_keys(value)){clear();if(!old.is_null()&&!suspended_)register_keys(old);return false;}applied_=value;suspended_=false;return true;}
void Hotkeys::suspend(){clear();suspended_=true;}
void Hotkeys::resume(){if(suspended_){clear();register_keys(applied_);suspended_=false;}}
}

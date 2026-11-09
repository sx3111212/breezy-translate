#include "app.hpp"
#include <dwmapi.h>
#include <shlwapi.h>

namespace breezy {
App* App::active_=nullptr;
static constexpr UINT dispatch=WM_APP+1,mouse_release=WM_APP+2,tray_message=WM_APP+3,mouse_down=WM_APP+4;
static constexpr int menu_read=1,menu_write=2,menu_manual=3,menu_settings=4,menu_follow=5,menu_bottom=6,menu_update=7,menu_about=8,menu_exit=9;
void round_window(HWND window){DWORD preference=2;DwmSetWindowAttribute(window,33,&preference,sizeof(preference));MARGINS margins{1,1,1,1};DwmExtendFrameIntoClientArea(window,&margins);}
HWND control(App& app,HWND parent,const wchar_t* type,const std::string& text,int id,int x,int y,int width,int height,DWORD style){
    HWND value=CreateWindowExW((type==std::wstring(L"EDIT")?WS_EX_CLIENTEDGE:0),type,wide(text).c_str(),WS_CHILD|WS_VISIBLE|style,x,y,width,height,parent,(HMENU)(INT_PTR)id,app.instance(),nullptr);
    SendMessageW(value,WM_SETFONT,(WPARAM)app.font(),TRUE);return value;
}
std::vector<Choice> language_choices(const App& app,bool source,bool interface_only){
    std::vector<Choice> items;if(source)items.push_back({"auto",app.t("sourceGuess")});
    for(auto& item:app.strings.languages()){if(interface_only&&std::find(Strings::interface_languages().begin(),Strings::interface_languages().end(),item.code)==Strings::interface_languages().end())continue;items.push_back({item.code,item.name});}return items;
}
std::vector<Choice> service_choices(const App& app,const Json& settings,bool word){
    std::vector<Choice> items;if(word)items.push_back({"Offline",app.t("offlineMode")});items.push_back({"AI",app.t("defaultAi")});
    for(auto& profile:settings.at("AiProfiles"))items.push_back({"ai:"+get(profile,"Id"),get(profile,"Name")+" · "+get(profile,"Model")});
    if(!word){items.push_back({"Azure","Azure Translator"});items.push_back({"MyMemory","MyMemory"});}return items;
}
void choices(HWND combo,const std::vector<Choice>& items,const std::string& selected){SendMessageW(combo,CB_RESETCONTENT,0,0);int index=0;for(size_t i=0;i<items.size();i++){SendMessageW(combo,CB_ADDSTRING,0,(LPARAM)wide(items[i].label).c_str());if(items[i].code==selected)index=(int)i;}SendMessageW(combo,CB_SETCURSEL,index,0);SendMessageW(combo,CB_SETDROPPEDWIDTH,450,0);}
std::string chosen(HWND combo,const std::vector<Choice>& items,const std::string& fallback){LRESULT index=SendMessageW(combo,CB_GETCURSEL,0,0);return index>=0&&(size_t)index<items.size()?items[(size_t)index].code:fallback;}
App::App(HINSTANCE instance,Path preferences_path,Path dictionary_path):instance_(instance),preferences(std::move(preferences_path)),dictionary(std::move(dictionary_path)){
    active_=this;auto prefs=preferences.snapshot();strings.language=get(prefs,"InterfaceLanguage","en");
    try{links=Json::parse(read_file(app_dir()/L"app-links.json",65536));}catch(...){links={{"ProjectUrl","https://github.com/sx3111212/breezy-translate"},{"ReleasesUrl","https://github.com/sx3111212/breezy-translate/releases/latest"},{"DonationUrl",""}};}
    Gdiplus::GdiplusStartupInput input;Gdiplus::GdiplusStartup(&graphics_,&input,nullptr);
    auto bytes=resource(11);Com<IStream> stream(SHCreateMemStream((const BYTE*)bytes.data(),(UINT)bytes.size()));
    if(stream){std::unique_ptr<Gdiplus::Bitmap> loaded(Gdiplus::Bitmap::FromStream(stream.get()));if(loaded&&loaded->GetLastStatus()==Gdiplus::Ok)image_.reset(loaded->Clone(0,0,(INT)loaded->GetWidth(),(INT)loaded->GetHeight(),PixelFormat32bppPARGB));}
    LOGFONTW font{};font.lfHeight=-17;font.lfWeight=FW_NORMAL;wcscpy_s(font.lfFaceName,L"Segoe UI");font_=CreateFontIndirectW(&font);font.lfWeight=FW_SEMIBOLD;font.lfHeight=-22;title_font_=CreateFontIndirectW(&font);
    WNDCLASSEXW main_class{sizeof(main_class)};main_class.lpfnWndProc=main_proc;main_class.hInstance=instance_;main_class.lpszClassName=L"Breezy.Native.Main";main_class.hIcon=LoadIconW(instance_,MAKEINTRESOURCEW(1));main_class.hCursor=LoadCursorW(nullptr,IDC_ARROW);RegisterClassExW(&main_class);
    auto bubble_class=main_class;bubble_class.lpfnWndProc=bubble_proc;bubble_class.lpszClassName=L"Breezy.Native.Bubble";RegisterClassExW(&bubble_class);
    main_=CreateWindowExW(WS_EX_TOOLWINDOW,main_class.lpszClassName,L"Breezy",WS_POPUP,0,0,0,0,nullptr,nullptr,instance_,this);
    bubble_=CreateWindowExW(WS_EX_LAYERED|WS_EX_TOPMOST|WS_EX_NOACTIVATE|WS_EX_TOOLWINDOW,bubble_class.lpszClassName,wide(t("bubbleAccessible")).c_str(),WS_POPUP,0,0,64,64,nullptr,nullptr,instance_,this);
    tooltip_=CreateWindowExW(WS_EX_TOPMOST,TOOLTIPS_CLASSW,nullptr,WS_POPUP|TTS_ALWAYSTIP|TTS_NOPREFIX,CW_USEDEFAULT,CW_USEDEFAULT,CW_USEDEFAULT,CW_USEDEFAULT,bubble_,nullptr,instance_,nullptr);tooltip_text_=wide(t("readAction"));TOOLINFOW hint{sizeof(hint)};hint.uFlags=TTF_IDISHWND|TTF_SUBCLASS;hint.hwnd=bubble_;hint.uId=(UINT_PTR)bubble_;hint.lpszText=tooltip_text_.data();SendMessageW(tooltip_,TTM_ADDTOOLW,0,(LPARAM)&hint);SendMessageW(tooltip_,TTM_SETMAXTIPWIDTH,0,400);
    taskbar_created_=RegisterWindowMessageW(L"TaskbarCreated");hotkeys_=std::make_unique<Hotkeys>(main_);
    if(!hotkeys_->apply(prefs))error(t("conflict"));
    accessibility_=std::make_unique<Worker>();network_=std::make_unique<Worker>();
    GetCursorPos(&released_);pinned_=flag(prefs,"BubblePinned");bottom_=flag(prefs,"BubbleBottom");
    install_tray();mouse_=SetWindowsHookExW(WH_MOUSE_LL,mouse_proc,instance_,0);SetTimer(main_,1,160,nullptr);
}
App::~App(){
    closing_=true;http.cancel();if(mouse_)UnhookWindowsHookEx(mouse_);mouse_=nullptr;
    if(network_)network_.reset();if(accessibility_)accessibility_.reset();
    MSG message;while(PeekMessageW(&message,main_,dispatch,dispatch,PM_REMOVE))delete (std::function<void()>*)message.lParam;
    result.reset();settings.reset();if(voice_)voice_->Speak(L"",SPF_PURGEBEFORESPEAK,nullptr);
    if(tray_.cbSize)Shell_NotifyIconW(NIM_DELETE,&tray_);hotkeys_.reset();
    if(IsWindow(bubble_))DestroyWindow(bubble_);if(IsWindow(main_))DestroyWindow(main_);
    image_.reset();if(font_)DeleteObject(font_);if(title_font_)DeleteObject(title_font_);if(graphics_)Gdiplus::GdiplusShutdown(graphics_);active_=nullptr;
}
int App::run(){MSG message;while(GetMessageW(&message,nullptr,0,0)>0){HWND dialog=settings&&IsWindow(settings->window())?settings->window():result&&IsWindowVisible(result->window())?result->window():nullptr;if(dialog&&IsDialogMessageW(dialog,&message))continue;TranslateMessage(&message);DispatchMessageW(&message);}return(int)message.wParam;}
void App::post(std::function<void()> callback){if(closing_)return;auto value=new std::function<void()>(std::move(callback));if(!PostMessageW(main_,dispatch,0,(LPARAM)value))delete value;}
void App::work(std::function<void()> callback){network_->post(std::move(callback));}
void App::install_tray(){tray_={};tray_.cbSize=sizeof(tray_);tray_.hWnd=main_;tray_.uID=1;tray_.uFlags=NIF_ICON|NIF_MESSAGE|NIF_TIP;tray_.uCallbackMessage=tray_message;tray_.hIcon=LoadIconW(instance_,MAKEINTRESOURCEW(1));wcsncpy_s(tray_.szTip,wide(t("app")).c_str(),_TRUNCATE);Shell_NotifyIconW(NIM_ADD,&tray_);tray_.uVersion=NOTIFYICON_VERSION_4;Shell_NotifyIconW(NIM_SETVERSION,&tray_);}
void App::render_bubble(){
    if(!bubble_||!image_)return;int size=64;HDC screen=GetDC(nullptr),memory=CreateCompatibleDC(screen);BITMAPINFO info{};info.bmiHeader.biSize=sizeof(BITMAPINFOHEADER);info.bmiHeader.biWidth=size;info.bmiHeader.biHeight=-size;info.bmiHeader.biPlanes=1;info.bmiHeader.biBitCount=32;info.bmiHeader.biCompression=BI_RGB;void* pixels=nullptr;HBITMAP bitmap=CreateDIBSection(screen,&info,DIB_RGB_COLORS,&pixels,nullptr,0);auto old=SelectObject(memory,bitmap);memset(pixels,0,size*size*4);
    {Gdiplus::Bitmap surface(size,size,size*4,PixelFormat32bppPARGB,(BYTE*)pixels);Gdiplus::Graphics graphics(&surface);graphics.SetSmoothingMode(Gdiplus::SmoothingModeAntiAlias);graphics.SetInterpolationMode(Gdiplus::InterpolationModeHighQualityBicubic);int pad=hover_?0:4;graphics.DrawImage(image_.get(),pad,pad,size-pad*2,size-pad*2);
        if(busy_){Gdiplus::Pen pen(Gdiplus::Color(230,91,113,230),3);graphics.DrawArc(&pen,8,8,48,48,(Gdiplus::REAL)(GetTickCount()%1800)/5,240);}
        else if(success_until_>GetTickCount()){Gdiplus::SolidBrush green(Gdiplus::Color(245,33,169,113));graphics.FillEllipse(&green,39,38,21,21);Gdiplus::Pen pen(Gdiplus::Color::White,2.5f);graphics.DrawLine(&pen,44,48,48,52);graphics.DrawLine(&pen,48,52,56,44);}}
    SIZE extent{size,size};POINT source{};BLENDFUNCTION blend{AC_SRC_OVER,0,255,AC_SRC_ALPHA};UpdateLayeredWindow(bubble_,screen,nullptr,&extent,memory,&source,0,&blend,ULW_ALPHA);SelectObject(memory,old);DeleteObject(bitmap);DeleteDC(memory);ReleaseDC(nullptr,screen);
}
void App::position_bubble(const Anchor& value){
    if(closing_||settings&&IsWindow(settings->window())||result&&IsWindowVisible(result->window())||hover_||dragging_)return;
    anchor_=value;if(!value.selection&&!value.editable){if(!pinned_)ShowWindow(bubble_,SW_HIDE);return;}
    auto prefs=preferences.snapshot();auto enabled=flag(prefs,"HotkeysEnabled",true)&&flag(prefs,value.selection?"ReadHotkeyEnabled":"InputHotkeyEnabled",true);tooltip_text_=wide(t(value.selection?"readAction":"writeAction")+" · "+(enabled?get(prefs,value.selection?"ReadHotkey":"InputHotkey"):t("shortcutDisabled")));TOOLINFOW hint{sizeof(hint)};hint.hwnd=bubble_;hint.uId=(UINT_PTR)bubble_;hint.lpszText=tooltip_text_.data();SendMessageW(tooltip_,TTM_UPDATETIPTEXTW,0,(LPARAM)&hint);
    last_external_=value.foreground;if(pinned_){ShowWindow(bubble_,SW_SHOWNOACTIVATE);return;}
    int x=value.selection?value.bounds.right+6:value.bounds.right-39;
    int y=value.selection?value.bounds.top-35:bottom_?value.bounds.bottom-39:value.bounds.top-16;
    MONITORINFO monitor{sizeof(monitor)};POINT point{x,y};GetMonitorInfoW(MonitorFromPoint(point,MONITOR_DEFAULTTONEAREST),&monitor);x=(int)std::clamp<LONG>(x,monitor.rcWork.left+2,std::max(monitor.rcWork.left+2,monitor.rcWork.right-66));y=(int)std::clamp<LONG>(y,monitor.rcWork.top+2,std::max(monitor.rcWork.top+2,monitor.rcWork.bottom-66));
    SetWindowPos(bubble_,HWND_TOPMOST,x,y,64,64,SWP_NOACTIVATE|SWP_SHOWWINDOW);render_bubble();
}
void App::poll(){
    if(polling_.exchange(true)||closing_)return;
    if(settings&&IsWindow(settings->window())||result&&IsWindowVisible(result->window())||hover_||dragging_){polling_=false;return;}
    POINT point=released_;accessibility_->post([this,point]{Anchor value;try{Automation automation;value=automation.anchor(point);}catch(...){}post([this,value]{polling_=false;position_bubble(value);});});
}
LRESULT CALLBACK App::mouse_proc(int code,WPARAM message,LPARAM data){if(code>=0&&active_&&(message==WM_LBUTTONUP||message==WM_RBUTTONUP||message==WM_MBUTTONUP||message==WM_LBUTTONDOWN||message==WM_RBUTTONDOWN||message==WM_MBUTTONDOWN)){auto item=(MSLLHOOKSTRUCT*)data;bool down=message==WM_LBUTTONDOWN||message==WM_RBUTTONDOWN||message==WM_MBUTTONDOWN;PostMessageW(active_->main_,down?mouse_down:mouse_release,(WPARAM)item->pt.x,(LPARAM)item->pt.y);}return CallNextHookEx(nullptr,code,message,data);}
LRESULT CALLBACK App::main_proc(HWND window,UINT message,WPARAM wparam,LPARAM lparam){
    auto app=(App*)GetWindowLongPtrW(window,GWLP_USERDATA);if(message==WM_NCCREATE){app=(App*)((CREATESTRUCTW*)lparam)->lpCreateParams;SetWindowLongPtrW(window,GWLP_USERDATA,(LONG_PTR)app);}if(!app)return DefWindowProcW(window,message,wparam,lparam);
    try{
        if(message==app->taskbar_created_&&message){app->install_tray();return 0;}
        switch(message){
        case dispatch:{std::unique_ptr<std::function<void()>> callback((std::function<void()>*)lparam);if(!app->closing_)(*callback)();return 0;}
        case mouse_down:case mouse_release:{POINT point{(LONG)wparam,(LONG)lparam};if(message==mouse_release)app->released_=point;if(app->result&&IsWindowVisible(app->result->window())){RECT bounds;GetWindowRect(app->result->window(),&bounds);HWND hit=WindowFromPoint(point);DWORD process=0;GetWindowThreadProcessId(hit,&process);if(!PtInRect(&bounds,point)&&process!=GetCurrentProcessId())app->result->hide();}return 0;}
        case WM_TIMER:if(app->result&&IsWindowVisible(app->result->window())&&app->result->original_window()){HWND current=GetForegroundWindow();DWORD process=0;GetWindowThreadProcessId(current,&process);if(current!=app->result->original_window()&&process!=GetCurrentProcessId())app->result->hide();}app->poll();if(app->busy_||app->success_until_>GetTickCount())app->render_bubble();return 0;
        case WM_HOTKEY:app->translate(wparam==0x5102);return 0;
        case tray_message:{UINT event=LOWORD(lparam);if(event==WM_CONTEXTMENU||event==WM_RBUTTONUP){POINT point;GetCursorPos(&point);app->menu(point);}else if(event==NIN_SELECT||event==WM_LBUTTONUP)app->open_settings();return 0;}
        case WM_CLOSE:DestroyWindow(window);return 0;
        case WM_DESTROY:app->closing_=true;app->http.cancel();PostQuitMessage(0);return 0;
        }
    }catch(const std::exception& error){app->error(error.what());}
    return DefWindowProcW(window,message,wparam,lparam);
}
LRESULT CALLBACK App::bubble_proc(HWND window,UINT message,WPARAM wparam,LPARAM lparam){
    auto app=(App*)GetWindowLongPtrW(window,GWLP_USERDATA);if(message==WM_NCCREATE){app=(App*)((CREATESTRUCTW*)lparam)->lpCreateParams;SetWindowLongPtrW(window,GWLP_USERDATA,(LONG_PTR)app);}if(!app)return DefWindowProcW(window,message,wparam,lparam);
    switch(message){
    case WM_MOUSEACTIVATE:return MA_NOACTIVATE;
    case WM_MOUSEMOVE:if(app->dragging_){POINT point;GetCursorPos(&point);if(abs(point.x-app->drag_start_.x)+abs(point.y-app->drag_start_.y)>4)app->pinned_=true;if(app->pinned_)SetWindowPos(window,HWND_TOPMOST,app->bubble_start_.x+point.x-app->drag_start_.x,app->bubble_start_.y+point.y-app->drag_start_.y,0,0,SWP_NOSIZE|SWP_NOACTIVATE);}else if(!app->hover_){app->hover_=true;TRACKMOUSEEVENT track{sizeof(track),TME_LEAVE,window,0};TrackMouseEvent(&track);app->render_bubble();}return 0;
    case WM_MOUSELEAVE:app->hover_=false;app->render_bubble();return 0;
    case WM_LBUTTONDOWN:{app->dragging_=true;GetCursorPos(&app->drag_start_);RECT rectangle;GetWindowRect(window,&rectangle);app->bubble_start_={rectangle.left,rectangle.top};SetCapture(window);return 0;}
    case WM_LBUTTONUP:{bool moved=app->pinned_&&(abs(app->bubble_start_.x+app->drag_start_.x)>0);POINT point;GetCursorPos(&point);moved=abs(point.x-app->drag_start_.x)+abs(point.y-app->drag_start_.y)>4;ReleaseCapture();app->dragging_=false;if(!moved)app->translate(!app->anchor_.selection);return 0;}
    case WM_RBUTTONUP:{POINT point;GetCursorPos(&point);app->menu(point);return 0;}
    }
    return DefWindowProcW(window,message,wparam,lparam);
}
void App::menu(POINT point){
    HWND previous=GetForegroundWindow();DWORD previous_process=0;GetWindowThreadProcessId(previous,&previous_process);if(previous&&previous_process!=GetCurrentProcessId())last_external_=previous;
    HMENU menu=CreatePopupMenu();auto add=[&](UINT flags,UINT id,const std::string& text){AppendMenuW(menu,flags,id,wide(text).c_str());};
    add(MF_STRING,menu_read,t("readAction"));add(MF_STRING,menu_write,t("writeAction"));add(MF_STRING,menu_manual,t("manual"));add(MF_SEPARATOR,0,"");add(MF_STRING,menu_settings,t("settings"));add(MF_STRING|(pinned_?0:MF_CHECKED),menu_follow,t("follow"));add(MF_STRING|(bottom_?MF_CHECKED:0),menu_bottom,t("bottom"));add(MF_STRING,menu_update,t("softwareUpdate"));add(MF_STRING,menu_about,t("donation"));add(MF_SEPARATOR,0,"");add(MF_STRING,menu_exit,t("exit"));
    SetForegroundWindow(main_);int id=TrackPopupMenu(menu,TPM_RETURNCMD|TPM_RIGHTBUTTON,point.x,point.y,0,main_,nullptr);DestroyMenu(menu);PostMessageW(main_,WM_NULL,0,0);
    switch(id){case menu_read:case menu_write:if(last_external_)SetForegroundWindow(last_external_);translate(id==menu_write);break;case menu_manual:open_manual();break;case menu_settings:open_settings();break;case menu_follow:pinned_=false;break;case menu_bottom:bottom_=!bottom_;break;case menu_update:open_settings(5);break;case menu_about:open_settings(6);break;case menu_exit:PostMessageW(main_,WM_CLOSE,0,0);break;}
}
void App::translate(bool writing,const std::string& manual,const std::string& source_override,const std::string& target_override,const std::string& provider_override,const std::string& model_override){
    cancel_translation();unsigned generation=++operation_;auto prefs=preferences.snapshot();busy_=true;render_bubble();POINT point=released_;
    accessibility_->post([this,generation,prefs,writing,manual,source_override,target_override,provider_override,model_override,point]{
        try{
            Target target;
            if(!manual.empty()){target.original=manual;target.writing=writing;}
            else{if(!wait_modifiers())throw std::runtime_error("Release the shortcut keys and try again.");Automation automation;try{target=automation.capture(writing,point);}catch(const std::exception& error){if(writing||std::string(error.what())!="noSelection")throw;target.original=selected_via_clipboard(main_);target.foreground=GetForegroundWindow();if(trim(target.original).empty())throw;}}
            if(generation!=operation_||closing_)return;
            auto source=source_override.empty()?(writing?get(prefs,"InputSourceLanguage","auto"):"auto"):source_override;
            auto destination=target_override.empty()?get(prefs,writing?"InputTarget":"ReadTarget","en"):target_override;
            auto provider=provider_override.empty()?resolve_provider(prefs,writing,Dictionary::is_word(target.original)):provider_override;
            network_->post([this,generation,prefs,target,writing,source,destination,provider,model_override]{
                if(generation!=operation_||closing_)return;
                try{auto effective=prefs;if(!model_override.empty()&&(provider=="AI"||provider.rfind("ai:",0)==0)){auto profile=find_profile(effective,provider.rfind("ai:",0)==0?provider.substr(3):resolve_profile(effective,writing,Dictionary::is_word(target.original)));for(auto& item:effective["AiProfiles"])if(get(item,"Id")==get(profile,"Id"))item["Model"]=model_override;}Translator translator(dictionary,strings,[this](auto& a,auto& b,auto& c,auto& d){return http.send(a,b,c,d);});auto translated=translator.translate(target.original,source,destination,effective,writing,provider);
                    if(generation!=operation_||closing_)return;
                    if(writing&&target.element){accessibility_->post([this,generation,target,translated,provider]{try{Automation automation;automation.replace(target,translated.text);post([this,generation,target,translated,provider]{if(generation!=operation_)return;busy_=false;success_until_=GetTickCount()+1200;if(!result)result=std::make_unique<ResultWindow>(*this);result->show(target,translated,provider);render_bubble();});}catch(const std::exception& error){auto message=std::string(error.what());post([this,generation,target,translated,provider,message]{if(generation!=operation_)return;busy_=false;if(!result)result=std::make_unique<ResultWindow>(*this);result->show(target,translated,provider);result->show_error(message);});}});}
                    else post([this,generation,target,translated,provider]{if(generation!=operation_)return;busy_=false;success_until_=GetTickCount()+1200;if(!result)result=std::make_unique<ResultWindow>(*this);result->show(target,translated,provider);render_bubble();});
                }catch(const std::exception& error){auto message=std::string(error.what());post([this,generation,message]{if(generation!=operation_)return;busy_=false;render_bubble();this->error(message);});}
            });
        }catch(const std::exception& error){auto message=std::string(error.what());post([this,generation,message]{if(generation!=operation_)return;busy_=false;render_bubble();this->error(message);});}
    });
}
void App::retranslate(const std::string& text,const std::string& source,const std::string& target,const std::string& provider,const std::string& model,bool writing){if(result)result->pending();translate(writing,text,source,target,provider,model);}
void App::cancel_translation(){++operation_;http.cancel();busy_=false;render_bubble();}
void App::dismiss_result(){cancel_translation();speak("",true);}
void App::open_manual(){if(!result)result=std::make_unique<ResultWindow>(*this);result->manual();}
void App::open_settings(int section){if(result)result->hide();if(settings&&IsWindow(settings->window())){settings->focus(section);return;}settings.reset();hotkeys_->suspend();settings=std::make_unique<SettingsWindow>(*this,section);}
void App::error(const std::string& message,HWND owner){auto localized=t(message);MessageBoxW(owner?owner:main_,wide(localized).c_str(),L"Breezy",MB_OK|MB_ICONINFORMATION);}
void App::speak(const std::string& text,bool stop,const std::string& language){
    if(speech_output){speech_output(text,language,stop);return;}
    if(stop){if(voice_)voice_->Speak(L"",SPF_ASYNC|SPF_PURGEBEFORESPEAK,nullptr);return;}
    if(trim(text).empty())return;
    if(!voice_&&FAILED(CoCreateInstance(CLSID_SpVoice,nullptr,CLSCTX_ALL,__uuidof(ISpVoice),(void**)voice_.put()))){error(t("missingVoice"));return;}
    if(voice_language_!=language){
        Com<IEnumSpObjectTokens> tokens;Com<ISpObjectTokenCategory> category;Com<ISpObjectToken> selected;
        LANGID wanted=PRIMARYLANGID(LANGIDFROMLCID(LocaleNameToLCID(wide(language).c_str(),0)));
        if(SUCCEEDED(CoCreateInstance(CLSID_SpObjectTokenCategory,nullptr,CLSCTX_ALL,__uuidof(ISpObjectTokenCategory),(void**)category.put()))&&SUCCEEDED(category->SetId(SPCAT_VOICES,FALSE))&&SUCCEEDED(category->EnumTokens(nullptr,nullptr,tokens.put()))){ULONG count=0;tokens->GetCount(&count);for(ULONG i=0;i<count&&!selected;i++){Com<ISpObjectToken> token;tokens->Item(i,token.put());Com<ISpDataKey> attributes;wchar_t* values=nullptr;if(!token||FAILED(token->OpenKey(L"Attributes",attributes.put()))||FAILED(attributes->GetStringValue(L"Language",&values)))continue;std::wistringstream languages(values?values:L"");CoTaskMemFree(values);std::wstring id;while(std::getline(languages,id,L';')){try{auto actual=PRIMARYLANGID(LANGIDFROMLCID(std::stoul(id,nullptr,16)));if(actual==wanted){selected=token;break;}}catch(...){}}}}
        if(!selected){error(t("missingVoice")+" "+strings.language_name(language));return;}voice_->SetVoice(selected.get());voice_language_=language;
    }
    if(FAILED(voice_->Speak(wide(text).c_str(),SPF_ASYNC|SPF_PURGEBEFORESPEAK|SPF_IS_NOT_XML,nullptr)))error(t("missingVoice"));
}
void App::replace(const Target& target,const std::string& text){
    if(!target.element){error("The original editor is unavailable. Copy the translation instead.");return;}
    if(result)ShowWindow(result->window(),SW_HIDE);SetForegroundWindow(target.foreground);accessibility_->post([this,target,text]{try{Automation automation;automation.replace(target,text);post([this]{success_until_=GetTickCount()+1200;});}catch(const std::exception& error){auto message=std::string(error.what());post([this,message]{this->error(message);});}});
}
void App::apply_preferences(const Json& value){
    if(flag(value,"HotkeysEnabled",true)){auto read=parse_hotkey(get(value,"ReadHotkey")),write=parse_hotkey(get(value,"InputHotkey"));bool a=flag(value,"ReadHotkeyEnabled",true),b=flag(value,"InputHotkeyEnabled",true);if(a&&!read.valid||b&&!write.valid||a&&b&&read.modifiers==write.modifiers&&read.key==write.key)throw std::runtime_error("keyRule");}
    auto previous=preferences.snapshot();if(!hotkeys_->apply(value))throw std::runtime_error("conflict");try{preferences.save(value);}catch(...){hotkeys_->apply(previous);throw;}strings.language=get(value,"InterfaceLanguage","en");Shell_NotifyIconW(NIM_DELETE,&tray_);install_tray();
}
int run_application(HINSTANCE instance){
    HANDLE mutex=CreateMutexW(nullptr,TRUE,L"Local\\EnglishHelper.Desktop");if(GetLastError()==ERROR_ALREADY_EXISTS){Strings strings;MessageBoxW(nullptr,wide(strings.text("alreadyRunning")).c_str(),L"Breezy",MB_OK);if(mutex)CloseHandle(mutex);return 0;}
    InitCommonControls();App app(instance);int result=app.run();if(mutex)CloseHandle(mutex);return result;
}
}

#include "app.hpp"
#include <richedit.h>

namespace breezy {

ResultWindow::~ResultWindow(){if(IsWindow(options_))DestroyWindow(options_);if(IsWindow(window_))DestroyWindow(window_);}
void ResultWindow::create(bool compact){
    if(window_&&compact_==compact&&ui_language_==app_.strings.language)return;if(IsWindow(options_))DestroyWindow(options_);options_=nullptr;if(IsWindow(window_))DestroyWindow(window_);compact_=compact;ui_language_=app_.strings.language;
    WNDCLASSEXW type{sizeof(type)};type.lpfnWndProc=proc;type.hInstance=app_.instance();type.lpszClassName=L"Breezy.Native.Result";type.hCursor=LoadCursorW(nullptr,IDC_ARROW);type.hbrBackground=(HBRUSH)GetStockObject(WHITE_BRUSH);type.hIcon=LoadIconW(app_.instance(),MAKEINTRESOURCEW(1));RegisterClassExW(&type);
    window_=CreateWindowExW(WS_EX_TOOLWINDOW|WS_EX_TOPMOST,type.lpszClassName,L"Breezy",WS_POPUP|WS_THICKFRAME|WS_CLIPCHILDREN,CW_USEDEFAULT,CW_USEDEFAULT,compact?360:420,compact?300:338,app_.window(),nullptr,app_.instance(),this);round_window(window_);
    auto close=control(app_,window_,L"BUTTON","\u00d7",IDCANCEL,0,0,29,31,WS_TABSTOP);theme::flat(close);SendMessageW(close,WM_SETFONT,(WPARAM)theme::font(24,false,L"Segoe UI"),TRUE);
    static HMODULE rich_edit=LoadLibraryW(L"Msftedit.dll");
    source_=control(app_,window_,compact?L"STATIC":rich_edit?L"RICHEDIT50W":L"EDIT","",result_source,18,9,300,43,compact?SS_LEFT:ES_MULTILINE|ES_WANTRETURN|ES_AUTOVSCROLL|WS_VSCROLL|WS_TABSTOP);
    SendMessageW(source_,WM_SETFONT,(WPARAM)theme::font(compact?28:16,false,compact?L"Segoe UI":L"Microsoft YaHei UI"),TRUE);
    target_=control(app_,window_,rich_edit?L"RICHEDIT50W":L"EDIT","",result_translation,18,97,324,120,ES_MULTILINE|ES_WANTRETURN|ES_AUTOVSCROLL|WS_VSCROLL|WS_TABSTOP|(compact?ES_READONLY:0));SendMessageW(target_,WM_SETFONT,(WPARAM)theme::font(14.667f),TRUE);
    if(rich_edit)for(HWND text:{compact?nullptr:source_,target_})if(text){SendMessageW(text,EM_SETBKGNDCOLOR,0,RGB(255,255,255));CHARFORMAT2W format{};format.cbSize=sizeof(format);format.dwMask=CFM_COLOR;format.crTextColor=RGB(38,65,98);SendMessageW(text,EM_SETCHARFORMAT,SCF_ALL,(LPARAM)&format);SendMessageW(text,EM_SETEVENTMASK,0,ENM_CHANGE);SendMessageW(text,EM_AUTOURLDETECT,FALSE,0);}
    phonetic_=control(app_,window_,L"STATIC","",120,18,63,180,22);SendMessageW(phonetic_,WM_SETFONT,(WPARAM)theme::font(13.333f,false,L"Segoe UI"),TRUE);
    direction_=control(app_,window_,L"BUTTON","",121,10,10,295,33,WS_TABSTOP);theme::flat(direction_);
    source_language_=control(app_,window_,WC_COMBOBOXW,"",result_source_language,0,0,1,300,CBS_DROPDOWNLIST);target_language_=control(app_,window_,WC_COMBOBOXW,"",result_target_language,0,0,1,300,CBS_DROPDOWNLIST);
    service_=control(app_,window_,WC_COMBOBOXW,"",result_service,0,0,1,300,CBS_DROPDOWNLIST);
    control(app_,window_,L"BUTTON",app_.t("translate"),124,0,0,65,33,WS_TABSTOP);theme::flat(GetDlgItem(window_,124));
    auto chooser=control(app_,window_,L"BUTTON","",122,18,220,259,36,WS_TABSTOP);SendMessageW(chooser,WM_SETFONT,(WPARAM)theme::font(10.667f),TRUE);
    auto retry=control(app_,window_,L"BUTTON",app_.t("retranslate"),result_translate,279,220,63,36,WS_TABSTOP);theme::flat(retry);
    model_=control(app_,window_,WC_COMBOBOXW,"",result_model,0,0,290,300,CBS_DROPDOWN|WS_TABSTOP|WS_VSCROLL);
    control(app_,window_,L"BUTTON",app_.t("aiLoad"),result_models,0,0,150,30,WS_TABSTOP);control(app_,window_,L"BUTTON",app_.t("aiApplyModel"),result_apply_model,0,0,150,30,WS_TABSTOP);
    for(int id:{result_copy,result_replace,result_speak,result_settings}){auto button=control(app_,window_,L"BUTTON",app_.t(id==result_copy?"copy":id==result_replace?"use":id==result_speak?"pronounce":"settings"),id,0,0,90,30,WS_TABSTOP);theme::flat(button);}
    auto_read_=control(app_,window_,L"BUTTON",app_.t("autoRead"),result_auto,18,260,125,28,BS_AUTOCHECKBOX|WS_TABSTOP);theme::toggle(auto_read_);
    status_=control(app_,window_,L"STATIC","",0,18,200,324,20,SS_LEFT);SendMessageW(status_,WM_SETFONT,(WPARAM)theme::font(10.667f),TRUE);
    provider_label_=control(app_,window_,L"STATIC","",123,200,268,142,20,SS_RIGHT);SendMessageW(provider_label_,WM_SETFONT,(WPARAM)theme::font(10.667f),TRUE);
    sources_=language_choices(app_,true);targets_=language_choices(app_);resize();
}
void ResultWindow::refresh_labels(){
    auto source=chosen(source_language_,sources_,source_code_),target=chosen(target_language_,targets_,target_code_);
    set_text(direction_,(source=="auto"?app_.strings.language_name(Strings::guess(window_text(source_)))+"?":app_.strings.language_name(source))+"  \u2192  "+app_.strings.language_name(target));
    auto provider=chosen(service_,services_,provider_);std::string name;for(auto& item:services_)if(item.code==provider)name=item.label;if(provider=="MyMemory")name+=" \u00b7 "+app_.t("online");set_text(GetDlgItem(window_,122),name+"   \u2304");set_text(provider_label_,provider=="Offline"?app_.t("offline"):name);
}
void ResultWindow::resize(){
    if(!window_||!target_)return;RECT client;GetClientRect(window_,&client);int width=client.right,height=client.bottom;
    MoveWindow(GetDlgItem(window_,IDCANCEL),width-47,compact_?13:10,29,31,TRUE);
    MoveWindow(source_,compact_?25:18,compact_?9:50,width-(compact_?72:36),compact_?43:55,TRUE);
    MoveWindow(target_,18,compact_?97:150,width-36,std::max(45,height-(compact_?97:150)-(window_text(status_).empty()?84:108)),TRUE);
    MoveWindow(GetDlgItem(window_,122),18,height-80,width-117,36,TRUE);MoveWindow(GetDlgItem(window_,result_translate),width-83,height-80,65,36,TRUE);
    HDC dc=GetDC(window_);auto old=SelectObject(dc,theme::font(12));SIZE label{};auto auto_label=wide(app_.t("autoRead"));GetTextExtentPoint32W(dc,auto_label.c_str(),(int)auto_label.size(),&label);SelectObject(dc,old);ReleaseDC(window_,dc);MoveWindow(auto_read_,18,height-40,std::clamp((int)label.cx+42,116,180),28,TRUE);MoveWindow(provider_label_,width-160,height-35,142,20,TRUE);
    ShowWindow(status_,window_text(status_).empty()?SW_HIDE:SW_SHOW);MoveWindow(status_,18,height-103,width-36,20,TRUE);MoveWindow(GetDlgItem(window_,result_copy),width-(original_.can_replace?153:70),height-40,52,32,TRUE);MoveWindow(GetDlgItem(window_,result_replace),width-101,height-40,83,32,TRUE);
    MoveWindow(GetDlgItem(window_,result_speak),compact_?86:18,compact_?57:111,30,30,TRUE);MoveWindow(GetDlgItem(window_,124),width-83,111,65,33,TRUE);ShowWindow(GetDlgItem(window_,124),compact_?SW_HIDE:SW_SHOW);
    for(int id:{result_source_language,result_target_language,result_service,result_model,result_models,result_apply_model,result_settings})ShowWindow(GetDlgItem(window_,id),SW_HIDE);
    ShowWindow(GetDlgItem(window_,result_copy),compact_?SW_HIDE:SW_SHOW);ShowWindow(GetDlgItem(window_,result_replace),!compact_&&original_.can_replace?SW_SHOW:SW_HIDE);ShowWindow(phonetic_,compact_?SW_SHOW:SW_HIDE);ShowWindow(provider_label_,compact_?SW_SHOW:SW_HIDE);ShowWindow(direction_,compact_?SW_HIDE:SW_SHOW);refresh_labels();InvalidateRect(window_,nullptr,FALSE);
}
void ResultWindow::show(const Target& target,const Translation& translated,const std::string& provider){
    Target saved=target;if(!saved.element&&original_.element&&original_.original==target.original)saved=original_;
    std::string local_entry;if(!saved.writing&&translated.target=="zh-CN"&&Dictionary::is_word(target.original))try{local_entry=app_.dictionary.lookup(target.original);}catch(...){}bool compact=!local_entry.empty();receiving_=true;create(compact);original_=saved;manual_=false;
    source_code_=translated.source;target_code_=translated.target;provider_=provider;auto settings=app_.preferences.snapshot();
    set_text(source_,target.original);auto definition=translated.text;std::string phonetic;
    if(compact&&translated.service=="ECDICT"&&definition.rfind("/",0)==0){auto end=definition.find("\r\n\r\n");if(end!=std::string::npos){phonetic=definition.substr(0,end);definition.erase(0,end+4);}}
    if(compact&&phonetic.empty()&&local_entry.rfind("/",0)==0){auto end=local_entry.find("\r\n");if(end!=std::string::npos)phonetic=local_entry.substr(0,end);}
    set_text(phonetic_,phonetic.empty()?app_.t("noPhonetic"):phonetic);set_text(target_,definition);set_text(status_,"");services_=service_choices(app_,settings,compact);choices(service_,services_,provider);choices(source_language_,sources_,source_code_);choices(target_language_,targets_,target_code_);
    SendMessageW(auto_read_,BM_SETCHECK,flag(settings,"AutoRead")?BST_CHECKED:BST_UNCHECKED,0);
    if(provider=="AI"||provider.rfind("ai:",0)==0){try{auto profile=find_profile(settings,provider.rfind("ai:",0)==0?provider.substr(3):resolve_profile(settings,saved.writing,compact));set_text(model_,translated.model.empty()?get(profile,"Model"):translated.model);}catch(...){}}
    EnableWindow(GetDlgItem(window_,result_replace),saved.can_replace&&translated.service!="ECDICT");EnableWindow(GetDlgItem(window_,result_translate),TRUE);EnableWindow(GetDlgItem(window_,124),TRUE);EnableWindow(GetDlgItem(window_,result_copy),TRUE);
    HDC measure=GetDC(window_);auto old=SelectObject(measure,theme::font(14.667f));RECT extent{0,0,compact?318:378,0};DrawTextW(measure,wide(definition).c_str(),-1,&extent,DT_CALCRECT|DT_WORDBREAK|DT_NOPREFIX);SelectObject(measure,old);ReleaseDC(window_,measure);int fitted_height=compact?std::clamp((int)extent.bottom+152,240,440)+38:std::clamp((int)extent.bottom+211,300,510)+38;SetWindowPos(window_,nullptr,0,0,compact?360:420,fitted_height,SWP_NOMOVE|SWP_NOZORDER|SWP_NOACTIVATE);resize();
    POINT point;GetCursorPos(&point);MONITORINFO monitor{sizeof(monitor)};GetMonitorInfoW(MonitorFromPoint(point,MONITOR_DEFAULTTONEAREST),&monitor);RECT rectangle;GetWindowRect(window_,&rectangle);int width=rectangle.right-rectangle.left,height=rectangle.bottom-rectangle.top;
    SetWindowPos(window_,HWND_TOPMOST,std::clamp(point.x+12,monitor.rcWork.left+8,std::max(monitor.rcWork.left+8,monitor.rcWork.right-width-8)),std::clamp(point.y+12,monitor.rcWork.top+8,std::max(monitor.rcWork.top+8,monitor.rcWork.bottom-height-8)),0,0,SWP_NOSIZE|SWP_NOACTIVATE|SWP_SHOWWINDOW);receiving_=false;
    if(flag(settings,"AutoRead"))speak();
}
void ResultWindow::manual(){receiving_=true;create(false);manual_=true;original_=Target{};auto prefs=app_.preferences.snapshot();source_code_="auto";target_code_=get(prefs,"ReadTarget","en");provider_=get(prefs,"ReadSentenceProvider","MyMemory");set_text(source_,"");set_text(target_,"");set_text(status_,"");sources_=language_choices(app_,true);targets_=language_choices(app_);services_=service_choices(app_,prefs);choices(source_language_,sources_,source_code_);choices(target_language_,targets_,target_code_);choices(service_,services_,provider_);EnableWindow(GetDlgItem(window_,result_replace),FALSE);SendMessageW(auto_read_,BM_SETCHECK,flag(prefs,"AutoRead")?BST_CHECKED:BST_UNCHECKED,0);ShowWindow(window_,SW_SHOWNORMAL);SetForegroundWindow(window_);SetFocus(source_);resize();receiving_=false;}
void ResultWindow::pending(){set_text(status_,app_.t("translate")+"…");EnableWindow(GetDlgItem(window_,result_translate),FALSE);EnableWindow(GetDlgItem(window_,124),FALSE);resize();}
void ResultWindow::speak(){auto language=compact_?"en":original_.writing?chosen(target_language_,targets_,target_code_):chosen(source_language_,sources_,source_code_);auto text=original_.writing?window_text(target_):window_text(source_);if(language=="auto")language=Strings::guess(text);app_.speak(text,false,language);}
void ResultWindow::show_error(const std::string& message){set_text(status_,message.empty()?"":app_.t(message));EnableWindow(GetDlgItem(window_,result_translate),TRUE);EnableWindow(GetDlgItem(window_,124),TRUE);resize();}
void ResultWindow::hide(){if(IsWindow(options_))ShowWindow(options_,SW_HIDE);if(window_)ShowWindow(window_,SW_HIDE);app_.dismiss_result();}
LRESULT CALLBACK ResultWindow::proc(HWND window,UINT message,WPARAM wparam,LPARAM lparam){
    auto self=(ResultWindow*)GetWindowLongPtrW(window,GWLP_USERDATA);if(message==WM_NCCREATE){self=(ResultWindow*)((CREATESTRUCTW*)lparam)->lpCreateParams;SetWindowLongPtrW(window,GWLP_USERDATA,(LONG_PTR)self);}if(!self)return DefWindowProcW(window,message,wparam,lparam);
    try{switch(message){
    case WM_NCCALCSIZE:return 0;
    case WM_NCHITTEST:{POINT point{(short)LOWORD(lparam),(short)HIWORD(lparam)};ScreenToClient(window,&point);if(point.y<50)return HTCAPTION;return HTCLIENT;}
    case WM_PAINT:{PAINTSTRUCT ps;auto dc=BeginPaint(window,&ps);RECT r;GetClientRect(window,&r);FillRect(dc,&r,(HBRUSH)GetStockObject(WHITE_BRUSH));HPEN pen=CreatePen(PS_SOLID,1,RGB(232,235,241));auto old=SelectObject(dc,pen);MoveToEx(dc,18,self->compact_?55:120,nullptr);LineTo(dc,r.right-18,self->compact_?55:120);SelectObject(dc,old);DeleteObject(pen);if(!self->compact_)theme::text(dc,self->app_.t("translation"),RECT{55,123,r.right-30,143},theme::font(10.667f),RGB(150,160,180));EndPaint(window,&ps);return 0;}
    case WM_CTLCOLORSTATIC:case WM_CTLCOLOREDIT:case WM_CTLCOLORLISTBOX:case WM_CTLCOLORBTN:{auto brush=theme::colors(message,wparam,lparam);if((HWND)lparam==self->target_||(HWND)lparam==self->source_)SetTextColor((HDC)wparam,RGB(38,65,98));if((HWND)lparam==self->phonetic_)SetTextColor((HDC)wparam,RGB(122,146,178));if((HWND)lparam==self->provider_label_)SetTextColor((HDC)wparam,RGB(180,188,202));return brush;}
    case WM_GETMINMAXINFO:((MINMAXINFO*)lparam)->ptMinTrackSize={self->compact_?360:420,self->compact_?278:338};return 0;
    case WM_SIZE:self->resize();return 0;
    case WM_ACTIVATE:if(LOWORD(wparam)==WA_INACTIVE&&!self->receiving_&&!self->menu_open_){HWND next=(HWND)lparam;if(next!=self->options_&&GetAncestor(next,GA_ROOTOWNER)!=window)self->hide();}return 0;
    case WM_CLOSE:self->hide();return 0;
    case WM_COMMAND:{int id=LOWORD(wparam),notification=HIWORD(wparam);
        if(id==125){self->open_model_options();return 0;}if(id==121){self->language_menu();return 0;}if(id==122){self->service_menu();return 0;}
        if(id==IDCANCEL){self->hide();return 0;}
        if(id==result_source&&notification==EN_CHANGE&&!self->receiving_){EnableWindow(GetDlgItem(window,result_replace),FALSE);EnableWindow(GetDlgItem(window,result_copy),FALSE);return 0;}
        if(id==result_copy)copy_text(window,window_text(self->target_));
        else if(id==result_speak)self->speak();
        else if(id==result_replace)self->app_.replace(self->original_,window_text(self->target_));
        else if(id==result_settings)self->app_.open_settings(2);
        else if(id==result_auto){auto value=self->app_.preferences.snapshot();bool enabled=SendMessageW(self->auto_read_,BM_GETCHECK,0,0)==BST_CHECKED;value["AutoRead"]=enabled;self->app_.preferences.save(value);if(enabled)self->speak();else self->app_.speak("",true);}
        else if(id==result_service&&notification==CBN_SELCHANGE){self->provider_=chosen(self->service_,self->services_);if(self->provider_=="AI"||self->provider_.rfind("ai:",0)==0){auto prefs=self->app_.preferences.snapshot();auto profile=find_profile(prefs,self->provider_.rfind("ai:",0)==0?self->provider_.substr(3):resolve_profile(prefs,self->original_.writing,self->compact_));set_text(self->model_,get(profile,"Model"));}self->resize();}
        else if(id==result_apply_model){if(IsWindow(self->options_))ShowWindow(self->options_,SW_HIDE);self->refresh_labels();}
        else if(id==result_translate||id==124){self->provider_=chosen(self->service_,self->services_,self->provider_);auto provider=self->provider_;if(provider=="AI"){auto prefs=self->app_.preferences.snapshot();auto id=resolve_profile(prefs,self->original_.writing,self->compact_);if(!id.empty())provider="ai:"+id;}self->app_.retranslate(window_text(self->source_),chosen(self->source_language_,self->sources_,self->source_code_),chosen(self->target_language_,self->targets_,self->target_code_),provider,window_text(self->model_),self->original_.writing);}
        else if(id==result_models){auto preferences=self->app_.preferences.snapshot();auto provider=chosen(self->service_,self->services_);auto profile=find_profile(preferences,provider.rfind("ai:",0)==0?provider.substr(3):resolve_profile(preferences,self->original_.writing,self->compact_));auto app=&self->app_;HWND recipient=window;self->pending();app->work([app,recipient,profile]{try{Translator translator(app->dictionary,app->strings,[app](auto&a,auto&b,auto&c,auto&d){return app->http.send(a,b,c,d);});auto models=translator.models(profile);app->post([app,recipient,models]{if(!app->result||app->result->window()!=recipient)return;auto combo=app->result->model_;auto current=window_text(combo);SendMessageW(combo,CB_RESETCONTENT,0,0);for(auto& model:models)SendMessageW(combo,CB_ADDSTRING,0,(LPARAM)wide(model).c_str());set_text(combo,current);app->result->show_error("");});}catch(const std::exception& error){auto message=std::string(error.what());app->post([app,recipient,message]{if(app->result&&app->result->window()==recipient)app->result->show_error(message);});}});}
        return 0;}
    }}catch(const std::exception& error){self->app_.error(error.what(),window);}
    return DefWindowProcW(window,message,wparam,lparam);
}
}

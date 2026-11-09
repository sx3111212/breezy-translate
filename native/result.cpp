#include "app.hpp"

namespace breezy {
enum {result_source=101,result_translation,result_source_language,result_target_language,result_service,result_translate,result_copy,result_replace,result_speak,result_auto,result_settings,result_model,result_models,result_apply_model};
ResultWindow::~ResultWindow(){if(IsWindow(window_))DestroyWindow(window_);}
void ResultWindow::create(bool compact){
    if(window_&&compact_==compact&&ui_language_==app_.strings.language)return;if(IsWindow(window_))DestroyWindow(window_);compact_=compact;ui_language_=app_.strings.language;
    WNDCLASSEXW type{sizeof(type)};type.lpfnWndProc=proc;type.hInstance=app_.instance();type.lpszClassName=L"Breezy.Native.Result";type.hCursor=LoadCursorW(nullptr,IDC_ARROW);type.hbrBackground=(HBRUSH)GetStockObject(WHITE_BRUSH);type.hIcon=LoadIconW(app_.instance(),MAKEINTRESOURCEW(1));RegisterClassExW(&type);
    window_=CreateWindowExW(WS_EX_TOOLWINDOW|WS_EX_TOPMOST,type.lpszClassName,L"Breezy",WS_POPUP|WS_CAPTION|WS_SYSMENU|WS_THICKFRAME,CW_USEDEFAULT,CW_USEDEFAULT,compact?420:680,compact?360:610,app_.window(),nullptr,app_.instance(),this);round_window(window_);
    source_=control(app_,window_,compact?L"STATIC":L"EDIT","",result_source,18,14,380,compact?34:96,compact?SS_LEFT:ES_MULTILINE|ES_WANTRETURN|ES_AUTOVSCROLL|WS_VSCROLL|WS_TABSTOP);
    if(compact)SendMessageW(source_,WM_SETFONT,(WPARAM)app_.font(true),TRUE);
    target_=control(app_,window_,L"EDIT","",result_translation,18,compact?58:160,380,compact?130:220,ES_MULTILINE|ES_WANTRETURN|ES_AUTOVSCROLL|WS_VSCROLL|WS_TABSTOP|(compact?ES_READONLY:0));
    source_language_=control(app_,window_,WC_COMBOBOXW,"",result_source_language,18,116,200,500,CBS_DROPDOWNLIST|WS_TABSTOP|WS_VSCROLL);
    target_language_=control(app_,window_,WC_COMBOBOXW,"",result_target_language,230,116,200,500,CBS_DROPDOWNLIST|WS_TABSTOP|WS_VSCROLL);
    service_=control(app_,window_,WC_COMBOBOXW,"",result_service,18,compact?200:390,240,500,CBS_DROPDOWNLIST|WS_TABSTOP|WS_VSCROLL);
    control(app_,window_,L"BUTTON",app_.t("retranslate"),result_translate,270,compact?200:390,120,32,WS_TABSTOP);
    model_=control(app_,window_,WC_COMBOBOXW,"",result_model,18,430,290,400,CBS_DROPDOWN|WS_TABSTOP|WS_VSCROLL);
    control(app_,window_,L"BUTTON",app_.t("aiLoad"),result_models,320,430,140,30,WS_TABSTOP);
    control(app_,window_,L"BUTTON",app_.t("aiApplyModel"),result_apply_model,470,430,160,30,WS_TABSTOP);
    control(app_,window_,L"BUTTON",app_.t("copy"),result_copy,18,compact?240:476,95,30,WS_TABSTOP);
    control(app_,window_,L"BUTTON",app_.t("use"),result_replace,123,476,110,30,WS_TABSTOP);
    control(app_,window_,L"BUTTON",app_.t("pronounce"),result_speak,compact?123:243,compact?240:476,120,30,WS_TABSTOP);
    control(app_,window_,L"BUTTON",app_.t("settings"),result_settings,compact?253:373,compact?240:476,125,30,WS_TABSTOP);
    auto_read_=control(app_,window_,L"BUTTON",app_.t("autoRead"),result_auto,18,compact?280:516,200,28,BS_AUTOCHECKBOX|WS_TABSTOP);
    status_=control(app_,window_,L"STATIC","",0,compact?218:230,compact?282:518,compact?175:395,45,SS_LEFT);
    sources_=language_choices(app_,true);targets_=language_choices(app_);resize();
}
void ResultWindow::resize(){
    if(!window_)return;RECT client{};GetClientRect(window_,&client);int width=client.right;
    MoveWindow(source_,18,14,width-36,compact_?34:96,TRUE);MoveWindow(target_,18,compact_?58:160,width-36,compact_?130:220,TRUE);
    if(!compact_){MoveWindow(source_language_,18,116,(width-48)/2,500,TRUE);MoveWindow(target_language_,30+(width-48)/2,116,(width-48)/2,500,TRUE);}
    ShowWindow(source_language_,compact_?SW_HIDE:SW_SHOW);ShowWindow(target_language_,compact_?SW_HIDE:SW_SHOW);ShowWindow(GetDlgItem(window_,result_replace),compact_?SW_HIDE:SW_SHOW);
    bool ai=provider_=="AI"||provider_.rfind("ai:",0)==0;
    for(int id:{result_model,result_models,result_apply_model})ShowWindow(GetDlgItem(window_,id),ai?SW_SHOW:SW_HIDE);
    if(compact_){int shift=ai?112:0;MoveWindow(service_,18,200,width-165,400,TRUE);MoveWindow(GetDlgItem(window_,result_translate),width-137,200,119,32,TRUE);if(ai){MoveWindow(model_,18,240,width-36,350,TRUE);MoveWindow(GetDlgItem(window_,result_models),18,276,(width-46)/2,30,TRUE);MoveWindow(GetDlgItem(window_,result_apply_model),28+(width-46)/2,276,(width-46)/2,30,TRUE);}MoveWindow(GetDlgItem(window_,result_copy),18,240+shift,95,30,TRUE);MoveWindow(GetDlgItem(window_,result_speak),123,240+shift,120,30,TRUE);MoveWindow(GetDlgItem(window_,result_settings),253,240+shift,125,30,TRUE);MoveWindow(auto_read_,18,280+shift,200,28,TRUE);MoveWindow(status_,218,282+shift,std::max(100,width-236),45,TRUE);RECT rectangle;GetWindowRect(window_,&rectangle);int height=360+shift;if(rectangle.bottom-rectangle.top!=height)SetWindowPos(window_,nullptr,0,0,rectangle.right-rectangle.left,height,SWP_NOMOVE|SWP_NOZORDER|SWP_NOACTIVATE|SWP_NOSENDCHANGING|SWP_NOCOPYBITS);}
}
void ResultWindow::show(const Target& target,const Translation& translated,const std::string& provider){
    Target saved=target;if(!saved.element&&original_.element&&original_.original==target.original)saved=original_;
    bool compact=!saved.writing&&Dictionary::is_word(target.original);receiving_=true;create(compact);original_=saved;manual_=false;
    source_code_=translated.source;target_code_=translated.target;provider_=provider;auto settings=app_.preferences.snapshot();
    set_text(source_,target.original);set_text(target_,translated.text);set_text(status_,translated.service);services_=service_choices(app_,settings,compact);choices(service_,services_,provider);choices(source_language_,sources_,source_code_);choices(target_language_,targets_,target_code_);
    SendMessageW(auto_read_,BM_SETCHECK,flag(settings,"AutoRead")?BST_CHECKED:BST_UNCHECKED,0);
    if(provider=="AI"||provider.rfind("ai:",0)==0){try{auto profile=find_profile(settings,provider.rfind("ai:",0)==0?provider.substr(3):resolve_profile(settings,saved.writing,compact));set_text(model_,translated.model.empty()?get(profile,"Model"):translated.model);}catch(...){}}
    EnableWindow(GetDlgItem(window_,result_replace),saved.can_replace&&translated.service!="ECDICT");EnableWindow(GetDlgItem(window_,result_translate),TRUE);EnableWindow(GetDlgItem(window_,result_copy),TRUE);resize();
    POINT point;GetCursorPos(&point);MONITORINFO monitor{sizeof(monitor)};GetMonitorInfoW(MonitorFromPoint(point,MONITOR_DEFAULTTONEAREST),&monitor);RECT rectangle;GetWindowRect(window_,&rectangle);int width=rectangle.right-rectangle.left,height=rectangle.bottom-rectangle.top;
    SetWindowPos(window_,HWND_TOPMOST,std::clamp(point.x+12,monitor.rcWork.left+8,std::max(monitor.rcWork.left+8,monitor.rcWork.right-width-8)),std::clamp(point.y+12,monitor.rcWork.top+8,std::max(monitor.rcWork.top+8,monitor.rcWork.bottom-height-8)),0,0,SWP_NOSIZE|SWP_NOACTIVATE|SWP_SHOWWINDOW);receiving_=false;
    if(flag(settings,"AutoRead"))speak();
}
void ResultWindow::manual(){receiving_=true;create(false);manual_=true;original_=Target{};auto prefs=app_.preferences.snapshot();source_code_="auto";target_code_=get(prefs,"ReadTarget","en");provider_=get(prefs,"ReadSentenceProvider","MyMemory");set_text(source_,"");set_text(target_,"");set_text(status_,"");sources_=language_choices(app_,true);targets_=language_choices(app_);services_=service_choices(app_,prefs);choices(source_language_,sources_,source_code_);choices(target_language_,targets_,target_code_);choices(service_,services_,provider_);EnableWindow(GetDlgItem(window_,result_replace),FALSE);SendMessageW(auto_read_,BM_SETCHECK,flag(prefs,"AutoRead")?BST_CHECKED:BST_UNCHECKED,0);ShowWindow(window_,SW_SHOWNORMAL);SetForegroundWindow(window_);SetFocus(source_);resize();receiving_=false;}
void ResultWindow::pending(){set_text(status_,app_.t("translate")+"…");EnableWindow(GetDlgItem(window_,result_translate),FALSE);}
void ResultWindow::speak(){auto language=compact_?"en":original_.writing?chosen(target_language_,targets_,target_code_):chosen(source_language_,sources_,source_code_);auto text=original_.writing?window_text(target_):window_text(source_);if(language=="auto")language=Strings::guess(text);app_.speak(text,false,language);}
void ResultWindow::show_error(const std::string& message){set_text(status_,app_.t(message));EnableWindow(GetDlgItem(window_,result_translate),TRUE);}
void ResultWindow::hide(){if(window_)ShowWindow(window_,SW_HIDE);app_.dismiss_result();}
LRESULT CALLBACK ResultWindow::proc(HWND window,UINT message,WPARAM wparam,LPARAM lparam){
    auto self=(ResultWindow*)GetWindowLongPtrW(window,GWLP_USERDATA);if(message==WM_NCCREATE){self=(ResultWindow*)((CREATESTRUCTW*)lparam)->lpCreateParams;SetWindowLongPtrW(window,GWLP_USERDATA,(LONG_PTR)self);}if(!self)return DefWindowProcW(window,message,wparam,lparam);
    try{switch(message){
    case WM_CTLCOLORSTATIC:SetBkMode((HDC)wparam,TRANSPARENT);return(LRESULT)GetStockObject(WHITE_BRUSH);
    case WM_GETMINMAXINFO:((MINMAXINFO*)lparam)->ptMinTrackSize={self->compact_?420:680,self->compact_?360:610};return 0;
    case WM_SIZE:self->resize();return 0;
    case WM_ACTIVATE:if(LOWORD(wparam)==WA_INACTIVE&&!self->receiving_){self->hide();}return 0;
    case WM_CLOSE:self->hide();return 0;
    case WM_COMMAND:{int id=LOWORD(wparam),notification=HIWORD(wparam);
        if(id==IDCANCEL){self->hide();return 0;}
        if(id==result_source&&notification==EN_CHANGE&&!self->receiving_){EnableWindow(GetDlgItem(window,result_replace),FALSE);EnableWindow(GetDlgItem(window,result_copy),FALSE);return 0;}
        if(id==result_copy)copy_text(window,window_text(self->target_));
        else if(id==result_speak)self->speak();
        else if(id==result_replace)self->app_.replace(self->original_,window_text(self->target_));
        else if(id==result_settings)self->app_.open_settings(2);
        else if(id==result_auto){auto value=self->app_.preferences.snapshot();bool enabled=SendMessageW(self->auto_read_,BM_GETCHECK,0,0)==BST_CHECKED;value["AutoRead"]=enabled;self->app_.preferences.save(value);if(enabled)self->speak();else self->app_.speak("",true);}
        else if(id==result_service&&notification==CBN_SELCHANGE){self->provider_=chosen(self->service_,self->services_);if(self->provider_=="AI"||self->provider_.rfind("ai:",0)==0){auto prefs=self->app_.preferences.snapshot();auto profile=find_profile(prefs,self->provider_.rfind("ai:",0)==0?self->provider_.substr(3):resolve_profile(prefs,self->original_.writing,self->compact_));set_text(self->model_,get(profile,"Model"));}self->resize();}
        else if(id==result_translate||id==result_apply_model){self->provider_=chosen(self->service_,self->services_,self->provider_);auto provider=self->provider_;if(provider=="AI"){auto prefs=self->app_.preferences.snapshot();auto id=resolve_profile(prefs,self->original_.writing,self->compact_);if(!id.empty())provider="ai:"+id;}self->app_.retranslate(window_text(self->source_),chosen(self->source_language_,self->sources_,self->source_code_),chosen(self->target_language_,self->targets_,self->target_code_),provider,window_text(self->model_),self->original_.writing);}
        else if(id==result_models){auto preferences=self->app_.preferences.snapshot();auto provider=chosen(self->service_,self->services_);auto profile=find_profile(preferences,provider.rfind("ai:",0)==0?provider.substr(3):resolve_profile(preferences,self->original_.writing,self->compact_));auto app=&self->app_;HWND recipient=window;self->pending();app->work([app,recipient,profile]{try{Translator translator(app->dictionary,app->strings,[app](auto&a,auto&b,auto&c,auto&d){return app->http.send(a,b,c,d);});auto models=translator.models(profile);app->post([app,recipient,models]{if(!app->result||app->result->window()!=recipient)return;auto combo=GetDlgItem(recipient,result_model);auto current=window_text(combo);SendMessageW(combo,CB_RESETCONTENT,0,0);for(auto& model:models)SendMessageW(combo,CB_ADDSTRING,0,(LPARAM)wide(model).c_str());set_text(combo,current);app->result->show_error("");});}catch(const std::exception& error){auto message=std::string(error.what());app->post([app,recipient,message]{if(app->result&&app->result->window()==recipient)app->result->show_error(message);});}});}
        return 0;}
    }}catch(const std::exception& error){self->app_.error(error.what(),window);}
    return DefWindowProcW(window,message,wparam,lparam);
}
}

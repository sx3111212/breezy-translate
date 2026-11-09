#include "app.hpp"

namespace breezy {
void ResultWindow::language_menu(){
    HMENU menu=CreatePopupMenu(),source=CreatePopupMenu(),target=CreatePopupMenu();
    auto selected_source=chosen(source_language_,sources_,source_code_),selected_target=chosen(target_language_,targets_,target_code_);
    for(size_t i=0;i<sources_.size();i++)AppendMenuW(source,MF_STRING|(sources_[i].code==selected_source?MF_CHECKED:0),1000+i,wide(sources_[i].label).c_str());
    for(size_t i=0;i<targets_.size();i++)AppendMenuW(target,MF_STRING|(targets_[i].code==selected_target?MF_CHECKED:0),2000+i,wide(targets_[i].label).c_str());
    AppendMenuW(menu,MF_POPUP,(UINT_PTR)source,wide(app_.t("source")).c_str());AppendMenuW(menu,MF_POPUP,(UINT_PTR)target,wide(app_.t("targetLanguage")).c_str());RECT bounds;GetWindowRect(direction_,&bounds);menu_open_=true;int id=TrackPopupMenu(menu,TPM_RETURNCMD,bounds.left,bounds.bottom,0,window_,nullptr);menu_open_=false;DestroyMenu(menu);
    if(id>=1000&&id<2000)SendMessageW(source_language_,CB_SETCURSEL,id-1000,0);else if(id>=2000)SendMessageW(target_language_,CB_SETCURSEL,id-2000,0);if(id){refresh_labels();EnableWindow(GetDlgItem(window_,result_copy),FALSE);EnableWindow(GetDlgItem(window_,result_replace),FALSE);}
}
void ResultWindow::service_menu(){
    HMENU menu=CreatePopupMenu();auto selected=chosen(service_,services_,provider_);for(size_t i=0;i<services_.size();i++)AppendMenuW(menu,MF_STRING|(services_[i].code==selected?MF_CHECKED:0),1000+i,wide(services_[i].label).c_str());
    if(selected=="AI"||selected.rfind("ai:",0)==0){AppendMenuW(menu,MF_SEPARATOR,0,nullptr);AppendMenuW(menu,MF_STRING,1,wide(app_.t("aiModel")+"…").c_str());}AppendMenuW(menu,MF_SEPARATOR,0,nullptr);AppendMenuW(menu,MF_STRING,2,wide(app_.t("settings")+"…").c_str());RECT bounds;GetWindowRect(GetDlgItem(window_,122),&bounds);menu_open_=true;int id=TrackPopupMenu(menu,TPM_RETURNCMD,bounds.left,bounds.bottom,0,window_,nullptr);menu_open_=false;DestroyMenu(menu);
    if(id>=1000){SendMessageW(service_,CB_SETCURSEL,id-1000,0);SendMessageW(window_,WM_COMMAND,MAKEWPARAM(result_service,CBN_SELCHANGE),0);return;}
    if(id==2){app_.open_settings(2);return;}if(id!=1)return;open_model_options();
}
void ResultWindow::open_model_options(){
    RECT bounds;GetWindowRect(GetDlgItem(window_,122),&bounds);
    if(!options_){WNDCLASSEXW type{sizeof(type)};type.hInstance=app_.instance();type.lpszClassName=L"Breezy.Native.ModelOptions";type.hCursor=LoadCursorW(nullptr,IDC_ARROW);type.hbrBackground=(HBRUSH)GetStockObject(WHITE_BRUSH);type.lpfnWndProc=[](HWND w,UINT m,WPARAM a,LPARAM b)->LRESULT{
        if(m==WM_NCCREATE)SetWindowLongPtrW(w,GWLP_USERDATA,(LONG_PTR)((CREATESTRUCTW*)b)->lpCreateParams);auto owner=(HWND)GetWindowLongPtrW(w,GWLP_USERDATA);
        if(m==WM_COMMAND){if(LOWORD(a)==IDCANCEL){ShowWindow(w,SW_HIDE);SetForegroundWindow(owner);return 0;}return SendMessageW(owner,m,a,b);}
        if(m>=WM_CTLCOLORMSGBOX&&m<=WM_CTLCOLORSTATIC)return theme::colors(m,a,b);
        if(m==WM_NCCALCSIZE)return 0;
        if(m==WM_CLOSE){ShowWindow(w,SW_HIDE);SetForegroundWindow(owner);return 0;}
        if(m==WM_ACTIVATE&&LOWORD(a)==WA_INACTIVE){ShowWindow(w,SW_HIDE);return 0;}
        return DefWindowProcW(w,m,a,b);
    };RegisterClassExW(&type);options_=CreateWindowExW(WS_EX_TOOLWINDOW|WS_EX_TOPMOST,type.lpszClassName,L"",WS_POPUP|WS_THICKFRAME|WS_CLIPCHILDREN,0,0,330,118,window_,nullptr,app_.instance(),window_);round_window(options_);
        control(app_,options_,L"STATIC",app_.t("aiModel"),0,12,8,300,20);for(int child:{result_model,result_models,result_apply_model})SetParent(GetDlgItem(window_,child),options_);
        MoveWindow(model_,12,32,306,300,TRUE);MoveWindow(GetDlgItem(options_,result_models),12,74,148,30,TRUE);MoveWindow(GetDlgItem(options_,result_apply_model),170,74,148,30,TRUE);
    }
    for(int child:{result_model,result_models,result_apply_model})ShowWindow(GetDlgItem(options_,child),SW_SHOW);MONITORINFO monitor{sizeof(monitor)};GetMonitorInfoW(MonitorFromRect(&bounds,MONITOR_DEFAULTTONEAREST),&monitor);SetWindowPos(options_,HWND_TOPMOST,std::clamp<LONG>(bounds.left,monitor.rcWork.left+8,monitor.rcWork.right-338),std::clamp<LONG>(bounds.bottom,monitor.rcWork.top+8,monitor.rcWork.bottom-126),0,0,SWP_NOSIZE|SWP_SHOWWINDOW);SetForegroundWindow(options_);SetFocus(model_);
}
}

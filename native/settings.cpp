#include "app.hpp"
#include <commdlg.h>

namespace breezy {
enum{nav=900,save=901,cancel=902,word_provider=1001,sentence_provider=1002,target_language=1003,shortcut_enabled=1004,shortcut=1005,manage=1006,source_language=1007,demo=1090,demo_pause=1091,demo_replay=1092,
    service_type=2001,profile_list=2002,preset=2003,profile_name=2004,protocol=2005,address=2006,api_key=2007,model=2008,default_profile=2009,load_models=2010,test_translation=2011,add_profile=2012,delete_profile=2013,
    azure_region=2101,azure_key=2102,memory_email=2201,memory_limits=2202,pack_import=3001,pack_url=3002,pack_download=3003,pack_folder=3004,pack_reload=3005,pack_list=3006,pack_delete=3007,pack_info=3008,
    interface_language=4001,release_page=5001,donate=6001,project=6002};
static const char* sections[]={"selectionFeature","inputFeature","service","offlineDictionary","generalSettings","softwareUpdate","donation"};
static const std::vector<Choice> protocols={{"ChatCompletions","Chat Completions"},{"Anthropic","Claude Messages"}};
static std::string chosen_provider(const Json& prefs,bool writing,bool word){auto provider=resolve_provider(prefs,writing,word);auto id=get(prefs,writing?"InputAiProfileId":word?"ReadWordAiProfileId":"ReadSentenceAiProfileId");return provider=="AI"&&!id.empty()?"ai:"+id:provider;}
static void store_provider(Json& prefs,bool writing,bool word,const std::string& value){auto provider=value.rfind("ai:",0)==0?"AI":value;const char* key=writing?"InputProvider":word?"ReadProvider":"ReadSentenceProvider";const char* profile=writing?"InputAiProfileId":word?"ReadWordAiProfileId":"ReadSentenceAiProfileId";prefs[key]=provider;prefs[profile]=value.rfind("ai:",0)==0?value.substr(3):"";}
SettingsWindow::SettingsWindow(App& app,int section):app_(app),draft_(app.preferences.snapshot()),section_(section){
    if(!flag(draft_,"HotkeysEnabled",true)){draft_["ReadHotkeyEnabled"]=false;draft_["InputHotkeyEnabled"]=false;}
    WNDCLASSEXW type{sizeof(type)};type.lpfnWndProc=proc;type.hInstance=app.instance();type.lpszClassName=L"Breezy.Native.Settings";type.hCursor=LoadCursorW(nullptr,IDC_ARROW);type.hbrBackground=(HBRUSH)GetStockObject(WHITE_BRUSH);type.hIcon=LoadIconW(app.instance(),MAKEINTRESOURCEW(1));RegisterClassExW(&type);auto host_type=type;host_type.lpfnWndProc=host_proc;host_type.lpszClassName=L"Breezy.Native.SettingsHost";RegisterClassExW(&host_type);
    window_=CreateWindowExW(WS_EX_APPWINDOW,type.lpszClassName,wide(app.t("appShort")+" · "+app.t("settings")).c_str(),WS_POPUP|WS_THICKFRAME|WS_CLIPCHILDREN,CW_USEDEFAULT,CW_USEDEFAULT,1018,747,app.window(),nullptr,app.instance(),this);round_window(window_);
    navigation_=control(app,window_,L"LISTBOX","",nav,16,60,190,600,LBS_NOTIFY|WS_TABSTOP|LBS_OWNERDRAWFIXED|LBS_HASSTRINGS|LBS_NOINTEGRALHEIGHT);
    SendMessageW(navigation_,LB_SETITEMHEIGHT,0,48);
    auto close=control(app,window_,L"BUTTON","\u00d7",903,966,3,38,32);theme::flat(close);SendMessageW(close,WM_SETFONT,(WPARAM)theme::font(23,false,L"Segoe UI"),TRUE);
    title_=control(app,window_,L"STATIC","",0,226,14,810,34,SS_CENTERIMAGE);SendMessageW(title_,WM_SETFONT,(WPARAM)app.font(true),TRUE);
    host_=CreateWindowExW(WS_EX_CONTROLPARENT,host_type.lpszClassName,L"",WS_CHILD|WS_VISIBLE|WS_CLIPCHILDREN|WS_VSCROLL,220,55,840,640,window_,nullptr,app.instance(),this);
    control(app,window_,L"BUTTON",app.t("save"),save,800,706,125,34,BS_DEFPUSHBUTTON|WS_TABSTOP);control(app,window_,L"BUTTON",app.t("cancel"),cancel,935,706,125,34,WS_TABSTOP);
    status_=control(app,window_,L"STATIC","",0,226,702,560,50,SS_LEFT);
    refresh_language();build(section);MONITORINFO monitor{sizeof(monitor)};GetMonitorInfoW(MonitorFromWindow(window_,MONITOR_DEFAULTTONEAREST),&monitor);RECT bounds;GetWindowRect(window_,&bounds);if(bounds.bottom-bounds.top>monitor.rcWork.bottom-monitor.rcWork.top)SetWindowPos(window_,nullptr,monitor.rcWork.left+10,monitor.rcWork.top+5,std::min(1100L,monitor.rcWork.right-monitor.rcWork.left-20),monitor.rcWork.bottom-monitor.rcWork.top-10,SWP_NOZORDER);layout();RECT outer;GetWindowRect(window_,&outer);int ow=outer.right-outer.left,oh=outer.bottom-outer.top;SetWindowPos(window_,nullptr,monitor.rcWork.left+(monitor.rcWork.right-monitor.rcWork.left-ow)/2,monitor.rcWork.top+(monitor.rcWork.bottom-monitor.rcWork.top-oh)/2,0,0,SWP_NOSIZE|SWP_NOZORDER);ShowWindow(window_,SW_SHOWNORMAL);SetForegroundWindow(window_);SetTimer(window_,1,33,nullptr);
}
SettingsWindow::~SettingsWindow(){if(IsWindow(window_))DestroyWindow(window_);}
void SettingsWindow::focus(int section){collect();build(std::clamp(section,0,6));ShowWindow(window_,SW_SHOWNORMAL);SetForegroundWindow(window_);}
void SettingsWindow::show_status(const std::string& message){set_text(status_,app_.t(message));}
HWND SettingsWindow::field(const wchar_t* type,const std::string& text,int id,int x,int y,int width,int height,DWORD style){auto value=control(app_,host_,type,text,id,x,y,width,height,style);fields_.push_back(value);return value;}
void SettingsWindow::label(const std::string& key,int x,int y,int width,int height){field(L"STATIC",app_.t(key),0,x,y,width,height,SS_LEFT);}
void SettingsWindow::refresh_language(){
    rebuilding_=true;SendMessageW(navigation_,LB_RESETCONTENT,0,0);for(auto key:sections)SendMessageW(navigation_,LB_ADDSTRING,0,(LPARAM)wide(app_.t(key)).c_str());SendMessageW(navigation_,LB_SETCURSEL,section_,0);set_text(window_,app_.t("appShort")+" · "+app_.t("settings"));set_text(GetDlgItem(window_,save),app_.t("save"));set_text(GetDlgItem(window_,cancel),app_.t("cancel"));rebuilding_=false;
}
void SettingsWindow::layout(){
    if(!window_||!host_||!navigation_)return;RECT client;GetClientRect(window_,&client);int width=client.right,height=client.bottom;
    MoveWindow(navigation_,25,54,184,std::max(100,height-143),TRUE);MoveWindow(title_,245,54,std::max(100,width-270),48,TRUE);MoveWindow(host_,221,102,std::max(100,width-246),std::max(100,height-191),TRUE);MoveWindow(status_,245,height-68,std::max(100,width-530),43,TRUE);MoveWindow(GetDlgItem(window_,save),width-155,height-72,130,32,TRUE);MoveWindow(GetDlgItem(window_,cancel),width-255,height-72,90,32,TRUE);MoveWindow(GetDlgItem(window_,903),width-48,3,38,32,TRUE);
    SCROLLINFO scroll{sizeof(scroll),SIF_RANGE|SIF_PAGE};scroll.nMin=0;scroll.nMax=(section_==3)?550:(section_<=2?450:550);scroll.nPage=std::max(100,height-191);SetScrollInfo(host_,SB_VERT,&scroll,TRUE);
}
void SettingsWindow::build(int section){
    rebuilding_=true;for(auto field:fields_)DestroyWindow(field);fields_.clear();SetScrollPos(host_,SB_VERT,0,TRUE);section_=section;SendMessageW(navigation_,LB_SETCURSEL,section,0);set_text(title_,app_.t(sections[section]));set_text(status_,"");
    languages_=language_choices(app_);sources_=language_choices(app_,true);interfaces_=language_choices(app_,false,true);services_=service_choices(app_,draft_);word_services_=service_choices(app_,draft_,true);
    auto combo=[&](int id,int y,const std::vector<Choice>& list,const std::string& selected){auto value=field(WC_COMBOBOXW,"",id,220,y,550,450,CBS_DROPDOWNLIST|WS_VSCROLL|WS_TABSTOP);choices(value,list,selected);return value;};
    if(section==0||section==1){
        bool writing=section==1;
        field(L"STATIC","",demo,24,22,720,170,SS_OWNERDRAW);
        auto pause=field(L"BUTTON","\u2161",demo_pause,668,29,28,26,WS_TABSTOP);auto replay=field(L"BUTTON","\u21ba",demo_replay,701,29,28,26,WS_TABSTOP);theme::flat(pause);theme::flat(replay);SendMessageW(pause,WM_SETFONT,(WPARAM)theme::font(14.667f,false,L"Segoe UI"),TRUE);SendMessageW(replay,WM_SETFONT,(WPARAM)theme::font(14.667f,false,L"Segoe UI"),TRUE);
        auto select=[&](int id,int y,int width,const std::vector<Choice>& items,const std::string& value){auto w=combo(id,y,items,value);MoveWindow(w,220,y,width,450,TRUE);};
        label(writing?"source":"targetLanguage",24,222);select(writing?source_language:target_language,218,310,writing?sources_:languages_,get(draft_,writing?"InputSourceLanguage":"ReadTarget",writing?"auto":"en"));
        label(writing?"targetLanguage":"wordService",24,266);select(writing?target_language:word_provider,262,writing?310:500,writing?languages_:word_services_,writing?get(draft_,"InputTarget","en"):chosen_provider(draft_,false,true));
        label(writing?"service":"sentenceService",24,310);select(sentence_provider,306,310,services_,chosen_provider(draft_,writing,false));
        auto manage_button=field(L"BUTTON",app_.t("manageServices"),manage,540,306,180,31,WS_TABSTOP);theme::flat(manage_button);
        auto quota=field(L"STATIC",app_.t("memoryQuotaHint"),0,220,339,500,23,SS_LEFT);SendMessageW(quota,WM_SETFONT,(WPARAM)theme::font(10.667f),TRUE);
        label("shortcuts",24,378);auto input=field(L"EDIT",get(draft_,writing?"InputHotkey":"ReadHotkey"),shortcut,220,374,260,32,ES_AUTOHSCROLL|WS_BORDER|WS_TABSTOP);SetWindowSubclass(input,hotkey_proc,1,(DWORD_PTR)this);SendMessageW(input,WM_SETFONT,(WPARAM)theme::font(16,false,L"Segoe UI"),TRUE);
        auto enabled=field(L"BUTTON",app_.t("enableShortcut"),shortcut_enabled,500,376,220,28,BS_AUTOCHECKBOX|WS_TABSTOP);SendMessageW(enabled,BM_SETCHECK,flag(draft_,writing?"InputHotkeyEnabled":"ReadHotkeyEnabled",true)?BST_CHECKED:BST_UNCHECKED,0);
        EnableWindow(input,flag(draft_,writing?"InputHotkeyEnabled":"ReadHotkeyEnabled",true));demo_frame_=0;demo_paused_=false;
    }else if(section==2){
        label("serviceType",24,28);auto category=combo(service_type,24,{{"AI",app_.t("aiServices")},{"Azure","Azure Translator"},{"MyMemory","MyMemory"}},service_type_);MoveWindow(category,220,24,340,450,TRUE);
        if(service_type_=="AI"){
            label("aiSaved",24,74,155);auto list=field(WC_COMBOBOXW,"",profile_list,184,70,360,400,CBS_DROPDOWNLIST|WS_VSCROLL|WS_TABSTOP);for(auto& item:draft_["AiProfiles"])SendMessageW(list,CB_ADDSTRING,0,(LPARAM)wide(get(item,"Name")+" \u00b7 "+get(item,"Model")).c_str());
            field(L"BUTTON",app_.t("aiAdd"),add_profile,554,70,60,30,WS_TABSTOP);field(L"BUTTON",app_.t("aiDelete"),delete_profile,619,70,60,30,WS_TABSTOP);
            label("aiPreset",24,118,155);auto preset_box=field(WC_COMBOBOXW,"",preset,184,114,330,420,CBS_DROPDOWNLIST|WS_VSCROLL|WS_TABSTOP);for(auto& item:presets())SendMessageW(preset_box,CB_ADDSTRING,0,(LPARAM)wide(item.name).c_str());
            auto protocol_box=field(WC_COMBOBOXW,"",protocol,524,114,155,300,CBS_DROPDOWNLIST|WS_TABSTOP);choices(protocol_box,protocols,"ChatCompletions");
            label("aiName",24,162,155);field(L"EDIT","",profile_name,184,158,495,28,ES_AUTOHSCROLL|WS_TABSTOP);
            label("aiAddress",24,206,155);field(L"EDIT","",address,184,202,495,28,ES_AUTOHSCROLL|WS_TABSTOP);
            label("aiKey",24,250,155);field(L"EDIT","",api_key,184,246,495,28,ES_AUTOHSCROLL|ES_PASSWORD|WS_TABSTOP);
            label("aiModel",24,294,155);field(WC_COMBOBOXW,"",model,184,290,350,420,CBS_DROPDOWN|WS_VSCROLL|WS_TABSTOP);
            field(L"BUTTON",app_.t("aiLoad"),load_models,544,290,135,30,WS_TABSTOP);field(L"BUTTON",app_.t("aiDefault"),default_profile,184,334,350,30,BS_AUTOCHECKBOX|WS_TABSTOP);field(L"BUTTON",app_.t("aiTest"),test_translation,544,334,135,30,WS_TABSTOP);
            field(L"STATIC",app_.t("aiHint"),0,24,380,655,78,SS_LEFT);for(int id:{add_profile,delete_profile,load_models,test_translation})theme::outline(GetDlgItem(host_,id));
            int index=profile_index_;if(index<0||index>=(int)draft_["AiProfiles"].size())index=draft_["AiProfiles"].empty()?-1:0;load_profile(index);
        }else if(service_type_=="Azure"){
            label("apiKey",24,78);field(L"EDIT","",azure_key,220,74,460,28,ES_AUTOHSCROLL|ES_PASSWORD|WS_TABSTOP);label("resourceRegion",24,128);field(L"EDIT",get(draft_,"AzureRegion"),azure_region,220,124,460,28,ES_AUTOHSCROLL|WS_TABSTOP);
            SendMessageW(GetDlgItem(host_,azure_key),EM_SETCUEBANNER,TRUE,(LPARAM)wide(app_.t(get(draft_,"ProtectedAzureKey").empty()?"apiKey":"keySaved")).c_str());
        }else{
            field(L"STATIC",app_.t("memoryIntro"),0,24,70,680,48,SS_LEFT);field(L"STATIC",app_.t("memoryQuota"),0,24,135,680,65,SS_LEFT);label("contactEmail",24,225);field(L"EDIT",get(draft_,"MyMemoryEmail"),memory_email,220,221,340,28,ES_AUTOHSCROLL|WS_TABSTOP);auto clear=field(L"BUTTON",app_.t("clearEmail"),2203,570,221,110,31,WS_TABSTOP);theme::flat(clear);field(L"STATIC",app_.t("memoryEmailNotice"),0,24,276,680,120,SS_LEFT);auto link=field(L"BUTTON",app_.t("memoryQuotaLink"),memory_limits,24,412,400,30,WS_TABSTOP);theme::flat(link);

        }
    }else if(section==3){
        auto heading=field(L"STATIC",app_.t("offlineMode"),0,24,22,680,30,SS_LEFT);SendMessageW(heading,WM_SETFONT,(WPARAM)theme::font(14,true),TRUE);
        packs_=app_.dictionary.packs();field(L"STATIC",std::to_string(app_.dictionary.count())+" "+app_.t("nativeWords")+" \u00b7 "+std::to_string(packs_.size())+" "+app_.t("nativePacks"),pack_info,24,58,680,54,SS_LEFT);
        auto list=field(WC_LISTVIEWW,"",pack_list,24,120,680,132,LVS_REPORT|LVS_SINGLESEL|LVS_SHOWSELALWAYS|WS_BORDER|WS_TABSTOP);ListView_SetExtendedListViewStyle(list,LVS_EX_FULLROWSELECT|LVS_EX_DOUBLEBUFFER);
        int column=0;for(auto key:{"nativePackFile","nativeEntries","nativePackStatus"}){auto text=wide(app_.t(key));LVCOLUMNW value{};value.mask=LVCF_TEXT|LVCF_WIDTH;value.pszText=text.data();value.cx=column==0?380:130;ListView_InsertColumn(list,column++,&value);}
        for(size_t i=0;i<packs_.size();i++){auto name=wide(packs_[i].name);LVITEMW row{};row.mask=LVIF_TEXT;row.iItem=(int)i;row.pszText=name.data();ListView_InsertItem(list,&row);auto count=wide(packs_[i].error.empty()?std::to_string(packs_[i].count):"-");auto state=wide(packs_[i].error.empty()?app_.t("nativeLoaded"):packs_[i].error);ListView_SetItemText(list,(int)i,1,count.data());ListView_SetItemText(list,(int)i,2,state.data());}
        auto remove=field(L"BUTTON",app_.t("nativeRemove"),pack_delete,474,260,230,34,WS_TABSTOP);EnableWindow(remove,FALSE);
        field(L"EDIT",utf8(app_.dictionary.directory().wstring()),0,24,308,680,28,ES_AUTOHSCROLL|ES_READONLY);
        field(L"BUTTON",app_.t("nativeFolder"),pack_folder,24,346,200,34,WS_TABSTOP);field(L"BUTTON",app_.t("nativeImport"),pack_import,236,346,230,34,WS_TABSTOP);field(L"BUTTON",app_.t("nativeReload"),pack_reload,478,346,226,34,WS_TABSTOP);
        field(L"EDIT","https://github.com/sx3111212/breezy-translate/releases/latest/download/ecdict-full-en-zh.json.gz",pack_url,24,396,460,30,ES_AUTOHSCROLL|WS_TABSTOP);field(L"BUTTON",app_.t("nativeDownload"),pack_download,498,394,206,34,WS_TABSTOP);field(L"STATIC",app_.t("nativePackHint"),0,24,438,680,112,SS_LEFT);for(int id:{pack_folder,pack_import,pack_reload,pack_download})theme::flat(GetDlgItem(host_,id));
    }else if(section==4){label("interface",24,34);auto box=combo(interface_language,30,interfaces_,get(draft_,"InterfaceLanguage","en"));MoveWindow(box,220,30,310,450,TRUE);}
    else if(section==5||section==6){
        bool about=section==6;field(L"STATIC",app_.t(about?"aboutIntro":"nativeUpdateIntro"),0,24,22,650,65,SS_LEFT);
        field(L"STATIC",about?app_.t("nativeSupportThanks"):app_.t("nativeVersion")+": " BREEZY_VERSION "\nWindows \u00b7 X64",0,24,97,650,54,SS_LEFT);
        field(L"STATIC",app_.t(about?"nativeNoDonation":"nativeUpdateHint"),0,24,162,650,65,SS_LEFT);
        auto action=field(L"BUTTON",app_.t(about?"nativeDonation":"nativeReleases"),about?donate:release_page,24,242,175,38,WS_TABSTOP);EnableWindow(action,secure_url(get(app_.links,about?"DonationUrl":"ReleasesUrl")));
        if(!about){field(L"BUTTON",app_.t("nativeNotes"),5002,215,242,175,38,WS_TABSTOP);std::string notes;try{notes=read_file(app_dir()/(app_.strings.language.rfind("zh",0)==0?L"CHANGELOG.md":L"CHANGELOG.en.md"),1024*1024);}catch(...){notes="";}auto text=field(L"EDIT",notes,5003,24,342,650,155,ES_MULTILINE|ES_READONLY|ES_AUTOVSCROLL|WS_VSCROLL);ShowWindow(text,SW_HIDE);}
        auto homepage=field(L"BUTTON",get(app_.links,"ProjectUrl"),project,24,302,650,30,WS_TABSTOP);theme::link(homepage);EnableWindow(homepage,secure_url(get(app_.links,"ProjectUrl")));
    }
    rebuilding_=false;layout();InvalidateRect(host_,nullptr,TRUE);
}
Json SettingsWindow::edited_profile(){
    if(profile_index_<0||profile_index_>=(int)draft_["AiProfiles"].size())throw std::runtime_error("configureAiFirst");auto value=profile_original_;
    value["Name"]=trim(window_text(GetDlgItem(host_,profile_name)));value["BaseUrl"]=trim(window_text(GetDlgItem(host_,address)));value["Model"]=trim(window_text(GetDlgItem(host_,model)));value["Protocol"]=chosen(GetDlgItem(host_,protocol),protocols,"ChatCompletions");
    auto key=window_text(GetDlgItem(host_,api_key));if(!key.empty())value["ProtectedKey"]=protect(key);else if(get(value,"BaseUrl")!=get(profile_original_,"BaseUrl")||get(value,"Protocol")!=get(profile_original_,"Protocol"))value["ProtectedKey"]="";
    if(get(value,"Name").empty())value["Name"]="AI";endpoint(get(value,"BaseUrl"),"models");return value;
}
void SettingsWindow::collect_profile(){if(service_type_!="AI"||profile_index_<0||profile_index_>=(int)draft_["AiProfiles"].size())return;auto value=edited_profile();draft_["AiProfiles"][profile_index_]=value;if(SendMessageW(GetDlgItem(host_,default_profile),BM_GETCHECK,0,0)==BST_CHECKED)draft_["DefaultAiProfileId"]=get(value,"Id");else if(get(draft_,"DefaultAiProfileId")==get(value,"Id"))draft_["DefaultAiProfileId"]="";}
void SettingsWindow::load_profile(int index){
    profile_index_=index;profile_original_=index>=0?draft_["AiProfiles"][index]:Json{{"Name",""},{"BaseUrl",""},{"Model",""},{"Protocol","ChatCompletions"},{"ProtectedKey",""}};
    SendMessageW(GetDlgItem(host_,profile_list),CB_SETCURSEL,index,0);set_text(GetDlgItem(host_,profile_name),get(profile_original_,"Name"));set_text(GetDlgItem(host_,address),get(profile_original_,"BaseUrl"));set_text(GetDlgItem(host_,api_key),"");set_text(GetDlgItem(host_,model),get(profile_original_,"Model"));choices(GetDlgItem(host_,protocol),protocols,get(profile_original_,"Protocol","ChatCompletions"));SendMessageW(GetDlgItem(host_,default_profile),BM_SETCHECK,get(draft_,"DefaultAiProfileId")==get(profile_original_,"Id")&&index>=0?BST_CHECKED:BST_UNCHECKED,0);
    int selected=(int)presets().size()-1;for(size_t i=0;i<presets().size();i++)if(presets()[i].url==get(profile_original_,"BaseUrl"))selected=(int)i;SendMessageW(GetDlgItem(host_,preset),CB_SETCURSEL,selected,0);for(int id:{preset,profile_name,address,api_key,model,protocol,default_profile,load_models,test_translation,delete_profile})EnableWindow(GetDlgItem(host_,id),index>=0);
}
void SettingsWindow::collect(){
    if(rebuilding_)return;
    if(section_==0||section_==1){bool writing=section_==1;draft_[writing?"InputTarget":"ReadTarget"]=chosen(GetDlgItem(host_,target_language),languages_,"en");draft_[writing?"InputHotkeyEnabled":"ReadHotkeyEnabled"]=SendMessageW(GetDlgItem(host_,shortcut_enabled),BM_GETCHECK,0,0)==BST_CHECKED;draft_[writing?"InputHotkey":"ReadHotkey"]=window_text(GetDlgItem(host_,shortcut));draft_["HotkeysEnabled"]=flag(draft_,"ReadHotkeyEnabled",true)||flag(draft_,"InputHotkeyEnabled",true);store_provider(draft_,writing,false,chosen(GetDlgItem(host_,sentence_provider),services_,"MyMemory"));if(writing)draft_["InputSourceLanguage"]=chosen(GetDlgItem(host_,source_language),sources_,"auto");else store_provider(draft_,false,true,chosen(GetDlgItem(host_,word_provider),word_services_,"Offline"));}
    else if(section_==2){if(service_type_=="AI")collect_profile();else if(service_type_=="Azure"){draft_["AzureRegion"]=trim(window_text(GetDlgItem(host_,azure_region)));auto key=window_text(GetDlgItem(host_,azure_key));if(!key.empty())draft_["ProtectedAzureKey"]=protect(key);}else draft_["MyMemoryEmail"]=validate_email(window_text(GetDlgItem(host_,memory_email)));}
    else if(section_==4)draft_["InterfaceLanguage"]=chosen(GetDlgItem(host_,interface_language),interfaces_,"en");
}
void SettingsWindow::command(int id,int notification){
    if(rebuilding_)return;
    if(id==shortcut_enabled){EnableWindow(GetDlgItem(host_,shortcut),SendMessageW(GetDlgItem(host_,shortcut_enabled),BM_GETCHECK,0,0)==BST_CHECKED);return;}
    if(id==manage){collect();auto selected=chosen(GetDlgItem(host_,sentence_provider),services_);service_type_=selected=="Azure"?"Azure":selected=="MyMemory"?"MyMemory":"AI";build(2);return;}
    if(id==demo_pause){demo_paused_=!demo_paused_;set_text(GetDlgItem(host_,demo_pause),demo_paused_?"\u25b6":"\u2161");return;}
    if(id==demo_replay){demo_frame_=0;demo_paused_=false;set_text(GetDlgItem(host_,demo_pause),"\u2161");InvalidateRect(GetDlgItem(host_,demo),nullptr,FALSE);return;}
    if((id==target_language||id==source_language)&&notification==CBN_SELCHANGE){collect();InvalidateRect(GetDlgItem(host_,demo),nullptr,FALSE);return;}
    if(id==interface_language&&notification==CBN_SELCHANGE){collect();app_.strings.language=get(draft_,"InterfaceLanguage","en");refresh_language();build(4);return;}
    if(id==service_type&&notification==CBN_SELCHANGE){collect();int selected=(int)SendMessageW(GetDlgItem(host_,service_type),CB_GETCURSEL,0,0);service_type_=selected==1?"Azure":selected==2?"MyMemory":"AI";build(2);return;}
    if(id==profile_list&&notification==CBN_SELCHANGE){int index=(int)SendMessageW(GetDlgItem(host_,profile_list),CB_GETCURSEL,0,0);collect_profile();load_profile(index);return;}
    if(id==preset&&notification==CBN_SELCHANGE){int index=(int)SendMessageW(GetDlgItem(host_,preset),CB_GETCURSEL,0,0);if(index>=0&&(size_t)index<presets().size()){set_text(GetDlgItem(host_,address),presets()[index].url);set_text(GetDlgItem(host_,api_key),"");profile_original_["ProtectedKey"]="";choices(GetDlgItem(host_,protocol),protocols,presets()[index].protocol);}return;}
    if(id==add_profile){collect_profile();GUID guid;CoCreateGuid(&guid);wchar_t buffer[48];StringFromGUID2(guid,buffer,48);Json value={{"Id",utf8(buffer)},{"Name","AI"},{"BaseUrl",presets()[0].url},{"Model",""},{"ProtectedKey",""},{"Protocol","ChatCompletions"}};draft_["AiProfiles"].push_back(value);profile_index_=(int)draft_["AiProfiles"].size()-1;build(2);return;}
    if(id==delete_profile){if(profile_index_>=0){auto removed=get(draft_["AiProfiles"][profile_index_],"Id");draft_["AiProfiles"].erase(draft_["AiProfiles"].begin()+profile_index_);for(auto key:{"DefaultAiProfileId","ReadWordAiProfileId","ReadSentenceAiProfileId","InputAiProfileId"})if(get(draft_,key)==removed)draft_[key]="";profile_index_=-1;build(2);}return;}
    if(id==load_models||id==test_translation){
        auto profile=edited_profile();auto prefs=draft_;prefs["AiProfiles"]=Json::array({profile});prefs["DefaultAiProfileId"]=get(profile,"Id");auto app=&app_;HWND recipient=window_;show_status(app_.t(id==load_models?"aiLoad":"aiTest")+"…");
        app_.work([app,recipient,profile,prefs,id]{try{Translator translator(app->dictionary,app->strings,[app](auto&a,auto&b,auto&c,auto&d){return app->http.send(a,b,c,d);});if(id==load_models){auto models=translator.models(profile);app->post([app,recipient,models,profile]{if(!app->settings||app->settings->window()!=recipient||app->settings->section_!=2||app->settings->service_type_!="AI")return;auto& self=*app->settings;if(get(self.profile_original_,"Id")!=get(profile,"Id")||window_text(GetDlgItem(self.host_,address))!=get(profile,"BaseUrl"))return;auto combo=GetDlgItem(self.host_,model);auto current=window_text(combo);SendMessageW(combo,CB_RESETCONTENT,0,0);for(auto& value:models)SendMessageW(combo,CB_ADDSTRING,0,(LPARAM)wide(value).c_str());set_text(combo,current);self.show_status(std::to_string(models.size())+" "+app->t("nativeModels"));});}else{auto translated=translator.translate("Hello","en","zh-CN",prefs,false,"AI");app->post([app,recipient,translated]{if(app->settings&&app->settings->window()==recipient)app->settings->show_status(translated.text);});}}catch(const std::exception& error){auto message=std::string(error.what());app->post([app,recipient,message]{if(app->settings&&app->settings->window()==recipient)app->settings->show_status(message);});}});return;
    }
    if(id==2203){set_text(GetDlgItem(host_,memory_email),"");return;}
    if(id==memory_limits){open_url("https://mymemory.translated.net/doc/usagelimits.php");return;}
    if(id==pack_folder){std::filesystem::create_directories(app_.dictionary.directory());ShellExecuteW(window_,L"open",app_.dictionary.directory().c_str(),nullptr,nullptr,SW_SHOWNORMAL);return;}
    if(id==pack_reload){app_.dictionary.reload();build(3);return;}
    if(id==pack_delete){int index=ListView_GetNextItem(GetDlgItem(host_,pack_list),-1,LVNI_SELECTED);if(index>=0&&(size_t)index<packs_.size()){if(MessageBoxW(window_,wide(packs_[index].name+"\n"+app_.t("aiDelete")+"?").c_str(),L"Breezy",MB_YESNO|MB_ICONQUESTION)==IDYES){app_.dictionary.remove(packs_[index].name);build(3);}}return;}
    if(id==pack_import){wchar_t path[32768]{};OPENFILENAMEW dialog{sizeof(dialog)};dialog.hwndOwner=window_;dialog.lpstrFilter=L"Dictionary packs (*.json.gz;*.csv)\0*.json.gz;*.csv\0All files\0*.*\0\0";dialog.lpstrFile=path;dialog.nMaxFile=32768;dialog.Flags=OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST|OFN_NOCHANGEDIR;if(GetOpenFileNameW(&dialog)){Path source(path);auto app=&app_;HWND recipient=window_;show_status(app_.t("nativeImporting"));app_.work([app,recipient,source]{try{app->dictionary.install(source);app->post([app,recipient]{if(app->settings&&app->settings->window()==recipient&&app->settings->section_==3)app->settings->build(3);});}catch(const std::exception& error){auto message=std::string(error.what());app->post([app,recipient,message]{if(app->settings&&app->settings->window()==recipient)app->settings->show_status(message);});}});}return;}
    if(id==pack_download){auto url=trim(window_text(GetDlgItem(host_,pack_url)));if(!secure_url(url))throw std::runtime_error("A direct HTTPS dictionary file URL is required.");auto name=url.substr(url.find_last_of('/')+1);auto query=name.find('?');if(query!=std::string::npos)name.resize(query);auto extension=lower(name);if(extension.size()<4||(extension.substr(extension.size()-4)!=".csv"&&(extension.size()<8||extension.substr(extension.size()-8)!=".json.gz")))throw std::runtime_error("A .json.gz or .csv dictionary URL is required.");auto app=&app_;HWND recipient=window_;show_status(app_.t("nativeDownloading"));app_.work([app,recipient,url,name]{try{auto reply=app->http.download(url);if(reply.status!=200)throw std::runtime_error("HTTP "+std::to_string(reply.status));app->dictionary.install_bytes(reply.body,name);app->post([app,recipient]{if(app->settings&&app->settings->window()==recipient&&app->settings->section_==3)app->settings->build(3);});}catch(const std::exception& error){auto message=std::string(error.what());app->post([app,recipient,message]{if(app->settings&&app->settings->window()==recipient)app->settings->show_status(message);});}});return;}
    if(id==5002){auto notes=GetDlgItem(host_,5003);bool visible=IsWindowVisible(notes);ShowWindow(notes,visible?SW_HIDE:SW_SHOW);set_text(GetDlgItem(host_,5002),app_.t(visible?"nativeNotes":"nativeHideNotes"));return;}
    if(id==release_page){open_url(get(app_.links,"ReleasesUrl"));return;}if(id==donate){open_url(get(app_.links,"DonationUrl"));return;}if(id==project){open_url(get(app_.links,"ProjectUrl"));return;}
}
LRESULT CALLBACK SettingsWindow::hotkey_proc(HWND window,UINT message,WPARAM wparam,LPARAM lparam,UINT_PTR,DWORD_PTR data){auto self=(SettingsWindow*)data;if(message==WM_KEYDOWN||message==WM_SYSKEYDOWN){auto value=record_hotkey((UINT)wparam);if(!value.empty())set_text(window,value);return 0;}if(message==WM_CHAR||message==WM_SYSCHAR)return 0;if(message==WM_NCDESTROY)RemoveWindowSubclass(window,hotkey_proc,1);return DefSubclassProc(window,message,wparam,lparam);}
LRESULT CALLBACK SettingsWindow::host_proc(HWND window,UINT message,WPARAM wparam,LPARAM lparam){auto self=(SettingsWindow*)GetWindowLongPtrW(window,GWLP_USERDATA);if(message==WM_NCCREATE){self=(SettingsWindow*)((CREATESTRUCTW*)lparam)->lpCreateParams;SetWindowLongPtrW(window,GWLP_USERDATA,(LONG_PTR)self);}if(!self)return DefWindowProcW(window,message,wparam,lparam);try{if(message>=WM_CTLCOLORMSGBOX&&message<=WM_CTLCOLORSTATIC)return theme::colors(message,wparam,lparam);if(message==WM_NOTIFY&&((NMHDR*)lparam)->idFrom==pack_list){EnableWindow(GetDlgItem(window,pack_delete),ListView_GetNextItem(GetDlgItem(window,pack_list),-1,LVNI_SELECTED)>=0);return 0;}if(message==WM_COMMAND){self->command(LOWORD(wparam),HIWORD(wparam));return 0;}if(message==WM_DRAWITEM&&((DRAWITEMSTRUCT*)lparam)->CtlID==demo){self->demo_paint(((DRAWITEMSTRUCT*)lparam)->hwndItem,((DRAWITEMSTRUCT*)lparam)->hDC);return TRUE;}if(message==WM_VSCROLL||message==WM_MOUSEWHEEL){SCROLLINFO scroll{sizeof(scroll),SIF_ALL};GetScrollInfo(window,SB_VERT,&scroll);int previous=scroll.nPos;int code=LOWORD(wparam);if(message==WM_MOUSEWHEEL)scroll.nPos-=(short)HIWORD(wparam)/WHEEL_DELTA*45;else if(code==SB_LINEUP)scroll.nPos-=30;else if(code==SB_LINEDOWN)scroll.nPos+=30;else if(code==SB_PAGEUP)scroll.nPos-=scroll.nPage;else if(code==SB_PAGEDOWN)scroll.nPos+=scroll.nPage;else if(code==SB_THUMBTRACK)scroll.nPos=scroll.nTrackPos;scroll.fMask=SIF_POS;SetScrollInfo(window,SB_VERT,&scroll,TRUE);GetScrollInfo(window,SB_VERT,&scroll);ScrollWindowEx(window,0,previous-scroll.nPos,nullptr,nullptr,nullptr,nullptr,SW_SCROLLCHILDREN|SW_INVALIDATE|SW_ERASE);return 0;}}catch(const std::exception& error){self->show_status(error.what());}return DefWindowProcW(window,message,wparam,lparam);}
LRESULT CALLBACK SettingsWindow::proc(HWND window,UINT message,WPARAM wparam,LPARAM lparam){
    auto self=(SettingsWindow*)GetWindowLongPtrW(window,GWLP_USERDATA);if(message==WM_NCCREATE){self=(SettingsWindow*)((CREATESTRUCTW*)lparam)->lpCreateParams;SetWindowLongPtrW(window,GWLP_USERDATA,(LONG_PTR)self);}if(!self)return DefWindowProcW(window,message,wparam,lparam);
    try{switch(message){
    case WM_NCCALCSIZE:return 0;
    case WM_NCHITTEST:{POINT point{(short)LOWORD(lparam),(short)HIWORD(lparam)};ScreenToClient(window,&point);RECT r;GetClientRect(window,&r);if(point.x<6)return HTLEFT;if(point.x>r.right-6)return HTRIGHT;if(point.y<4)return HTTOP;if(point.y>r.bottom-6)return HTBOTTOM;if(point.y<38)return HTCAPTION;return HTCLIENT;}
    case WM_ERASEBKGND:return 1;
    case WM_PAINT:{PAINTSTRUCT ps;auto dc=BeginPaint(window,&ps);RECT r;GetClientRect(window,&r);FillRect(dc,&r,(HBRUSH)GetStockObject(WHITE_BRUSH));DrawIconEx(dc,20,10,LoadIconW(self->app_.instance(),MAKEINTRESOURCEW(1)),16,16,0,nullptr,DI_NORMAL);theme::text(dc,window_text(window),RECT{42,3,r.right-70,34},theme::font(10.667f),RGB(95,105,123));EndPaint(window,&ps);return 0;}
    case WM_CTLCOLORSTATIC:case WM_CTLCOLOREDIT:case WM_CTLCOLORLISTBOX:case WM_CTLCOLORBTN:return theme::colors(message,wparam,lparam);
    case WM_DRAWITEM:if(((DRAWITEMSTRUCT*)lparam)->CtlID==nav){theme::navigation(*(DRAWITEMSTRUCT*)lparam);return TRUE;}break;
    case WM_MEASUREITEM:((MEASUREITEMSTRUCT*)lparam)->itemHeight=48;return TRUE;
    case WM_GETMINMAXINFO:((MINMAXINFO*)lparam)->ptMinTrackSize={978,647};return 0;
    case WM_SIZE:self->layout();return 0;
    case WM_COMMAND:if(LOWORD(wparam)==nav&&HIWORD(wparam)==LBN_SELCHANGE&&!self->rebuilding_){int section=(int)SendMessageW(self->navigation_,LB_GETCURSEL,0,0);self->collect();self->build(section);return 0;}else if(LOWORD(wparam)==save){self->collect();self->app_.apply_preferences(self->draft_);DestroyWindow(window);return 0;}else if(LOWORD(wparam)==cancel||LOWORD(wparam)==903||LOWORD(wparam)==IDCANCEL){DestroyWindow(window);return 0;}break;
    case WM_TIMER:if((self->section_==0||self->section_==1)&&!self->demo_paused_){self->demo_frame_++;InvalidateRect(GetDlgItem(self->host_,demo),nullptr,FALSE);}return 0;
    case WM_CLOSE:DestroyWindow(window);return 0;
    case WM_DESTROY:self->app_.strings.language=get(self->app_.preferences.snapshot(),"InterfaceLanguage","en");self->app_.hotkeys_->resume();self->window_=nullptr;return 0;
    }}catch(const std::exception& error){self->show_status(error.what());SendMessageW(self->navigation_,LB_SETCURSEL,self->section_,0);}return DefWindowProcW(window,message,wparam,lparam);
}
}

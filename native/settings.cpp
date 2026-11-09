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
    WNDCLASSEXW type{sizeof(type)};type.lpfnWndProc=proc;type.hInstance=app.instance();type.lpszClassName=L"Breezy.Native.Settings";type.hCursor=LoadCursorW(nullptr,IDC_ARROW);type.hbrBackground=(HBRUSH)GetStockObject(WHITE_BRUSH);type.hIcon=LoadIconW(app.instance(),MAKEINTRESOURCEW(1));RegisterClassExW(&type);auto host_type=type;host_type.lpfnWndProc=host_proc;host_type.lpszClassName=L"Breezy.Native.SettingsHost";RegisterClassExW(&host_type);
    window_=CreateWindowExW(WS_EX_APPWINDOW,type.lpszClassName,wide(app.t("appShort")+" · "+app.t("settings")).c_str(),WS_OVERLAPPEDWINDOW,CW_USEDEFAULT,CW_USEDEFAULT,1100,790,app.window(),nullptr,app.instance(),this);round_window(window_);
    navigation_=control(app,window_,L"LISTBOX","",nav,16,60,190,600,LBS_NOTIFY|WS_TABSTOP|WS_BORDER);
    title_=control(app,window_,L"STATIC","",0,226,14,810,34);SendMessageW(title_,WM_SETFONT,(WPARAM)app.font(true),TRUE);
    host_=CreateWindowExW(WS_EX_CONTROLPARENT,host_type.lpszClassName,L"",WS_CHILD|WS_VISIBLE|WS_CLIPCHILDREN|WS_VSCROLL,220,55,840,640,window_,nullptr,app.instance(),this);
    control(app,window_,L"BUTTON",app.t("save"),save,800,706,125,34,BS_DEFPUSHBUTTON|WS_TABSTOP);control(app,window_,L"BUTTON",app.t("cancel"),cancel,935,706,125,34,WS_TABSTOP);
    status_=control(app,window_,L"STATIC","",0,226,702,560,50,SS_LEFT);
    refresh_language();build(section);MONITORINFO monitor{sizeof(monitor)};GetMonitorInfoW(MonitorFromWindow(window_,MONITOR_DEFAULTTONEAREST),&monitor);RECT bounds;GetWindowRect(window_,&bounds);if(bounds.bottom-bounds.top>monitor.rcWork.bottom-monitor.rcWork.top)SetWindowPos(window_,nullptr,monitor.rcWork.left+10,monitor.rcWork.top+5,std::min(1100L,monitor.rcWork.right-monitor.rcWork.left-20),monitor.rcWork.bottom-monitor.rcWork.top-10,SWP_NOZORDER);layout();ShowWindow(window_,SW_SHOWNORMAL);SetForegroundWindow(window_);SetTimer(window_,1,250,nullptr);
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
    if(!window_||!host_||!navigation_)return;RECT client;GetClientRect(window_,&client);int width=client.right,height=client.bottom;MoveWindow(navigation_,16,60,190,std::max(100,height-85),TRUE);MoveWindow(title_,226,14,std::max(100,width-245),34,TRUE);MoveWindow(host_,220,55,std::max(100,width-236),std::max(100,height-125),TRUE);MoveWindow(status_,226,height-60,std::max(100,width-530),50,TRUE);MoveWindow(GetDlgItem(window_,save),width-285,height-55,125,34,TRUE);MoveWindow(GetDlgItem(window_,cancel),width-150,height-55,125,34,TRUE);SCROLLINFO scroll{sizeof(scroll),SIF_RANGE|SIF_PAGE};scroll.nMin=0;scroll.nMax=620;scroll.nPage=std::max(100,height-125);SetScrollInfo(host_,SB_VERT,&scroll,TRUE);
}
void SettingsWindow::build(int section){
    rebuilding_=true;for(auto field:fields_)DestroyWindow(field);fields_.clear();SetScrollPos(host_,SB_VERT,0,TRUE);section_=section;SendMessageW(navigation_,LB_SETCURSEL,section,0);set_text(title_,app_.t(sections[section]));set_text(status_,"");
    languages_=language_choices(app_);sources_=language_choices(app_,true);interfaces_=language_choices(app_,false,true);services_=service_choices(app_,draft_);word_services_=service_choices(app_,draft_,true);
    auto combo=[&](int id,int y,const std::vector<Choice>& list,const std::string& selected){auto value=field(WC_COMBOBOXW,"",id,220,y,550,450,CBS_DROPDOWNLIST|WS_VSCROLL|WS_TABSTOP);choices(value,list,selected);return value;};
    if(section==0||section==1){
        bool writing=section==1;
        if(writing){label("source",20,20);combo(source_language,18,sources_,get(draft_,"InputSourceLanguage","auto"));label("targetLanguage",20,78);combo(target_language,76,languages_,get(draft_,"InputTarget","en"));label("service",20,136);combo(sentence_provider,134,services_,chosen_provider(draft_,true,false));}
        else{label("wordService",20,20);combo(word_provider,18,word_services_,chosen_provider(draft_,false,true));label("sentenceService",20,78);combo(sentence_provider,76,services_,chosen_provider(draft_,false,false));label("targetLanguage",20,136);combo(target_language,134,languages_,get(draft_,"ReadTarget","en"));}
        auto enabled=field(L"BUTTON",app_.t("enableShortcut"),shortcut_enabled,20,190,750,30,BS_AUTOCHECKBOX|WS_TABSTOP);SendMessageW(enabled,BM_SETCHECK,flag(draft_,writing?"InputHotkeyEnabled":"ReadHotkeyEnabled",true)?BST_CHECKED:BST_UNCHECKED,0);
        label(writing?"writeKey":"readKey",20,235);auto input=field(L"EDIT",get(draft_,writing?"InputHotkey":"ReadHotkey"),shortcut,220,230,310,30,ES_AUTOHSCROLL|WS_TABSTOP);SetWindowSubclass(input,hotkey_proc,1,(DWORD_PTR)this);
        field(L"BUTTON",app_.t("manageServices"),manage,550,230,220,32,WS_TABSTOP);
        field(L"STATIC",app_.t("keyHint"),0,20,277,750,44,SS_LEFT);
        field(L"STATIC","",demo,20,335,750,170,SS_OWNERDRAW);field(L"BUTTON",app_.t("pauseDemo"),demo_pause,20,520,325,32,WS_TABSTOP);field(L"BUTTON",app_.t("replayDemo"),demo_replay,365,520,405,32,WS_TABSTOP);
        demo_frame_=0;demo_paused_=false;
    }else if(section==2){
        label("serviceType",20,20);combo(service_type,18,{{"AI",app_.t("aiServices")},{"Azure","Azure Translator"},{"MyMemory","MyMemory"}},service_type_);
        if(service_type_=="AI"){
            auto list=field(WC_COMBOBOXW,"",profile_list,20,70,470,400,CBS_DROPDOWNLIST|WS_VSCROLL|WS_TABSTOP);for(auto& item:draft_["AiProfiles"])SendMessageW(list,CB_ADDSTRING,0,(LPARAM)wide(get(item,"Name")+" · "+get(item,"Model")).c_str());
            field(L"BUTTON",app_.t("aiAdd"),add_profile,510,70,125,30,WS_TABSTOP);field(L"BUTTON",app_.t("aiDelete"),delete_profile,645,70,125,30,WS_TABSTOP);
            label("aiPreset",20,122);auto preset_box=field(WC_COMBOBOXW,"",preset,220,118,550,420,CBS_DROPDOWNLIST|WS_VSCROLL|WS_TABSTOP);for(auto& item:presets())SendMessageW(preset_box,CB_ADDSTRING,0,(LPARAM)wide(item.name).c_str());
            label("aiName",20,170);field(L"EDIT","",profile_name,220,166,550,30,ES_AUTOHSCROLL|WS_TABSTOP);
            label("aiAddress",20,214);field(L"EDIT","",address,220,210,550,30,ES_AUTOHSCROLL|WS_TABSTOP);
            label("aiKey",20,258);field(L"EDIT","",api_key,220,254,550,30,ES_AUTOHSCROLL|ES_PASSWORD|WS_TABSTOP);
            label("aiModel",20,302);field(WC_COMBOBOXW,"",model,220,298,550,420,CBS_DROPDOWN|WS_VSCROLL|WS_TABSTOP);
            field(L"STATIC",app_.t("nativeProtocol"),0,20,346,180,26);auto protocol_box=field(WC_COMBOBOXW,"",protocol,220,342,550,300,CBS_DROPDOWNLIST|WS_TABSTOP);choices(protocol_box,protocols,"ChatCompletions");
            field(L"BUTTON",app_.t("aiDefault"),default_profile,220,389,550,30,BS_AUTOCHECKBOX|WS_TABSTOP);
            field(L"BUTTON",app_.t("aiLoad"),load_models,220,435,260,34,WS_TABSTOP);field(L"BUTTON",app_.t("aiTest"),test_translation,495,435,275,34,WS_TABSTOP);
            field(L"STATIC",app_.t("aiHint"),0,20,490,750,115,SS_LEFT);
            int index=profile_index_;if(index<0||index>=(int)draft_["AiProfiles"].size())index=draft_["AiProfiles"].empty()?-1:0;load_profile(index);
        }else if(service_type_=="Azure"){
            label("resourceRegion",20,90);field(L"EDIT",get(draft_,"AzureRegion"),azure_region,220,86,550,30,ES_AUTOHSCROLL|WS_TABSTOP);label("apiKey",20,140);field(L"EDIT","",azure_key,220,136,550,30,ES_AUTOHSCROLL|ES_PASSWORD|WS_TABSTOP);field(L"STATIC",app_.t(get(draft_,"ProtectedAzureKey").empty()?"requiredKey":"keySaved"),0,20,200,750,90,SS_LEFT);field(L"STATIC",app_.t("commercialHint"),0,20,310,750,120,SS_LEFT);
        }else{
            field(L"STATIC",app_.t("memoryIntro"),0,20,80,750,70,SS_LEFT);field(L"STATIC",app_.t("memoryQuota"),0,20,160,750,90,SS_LEFT);label("contactEmail",20,270,750,30);field(L"EDIT",get(draft_,"MyMemoryEmail"),memory_email,20,310,750,30,ES_AUTOHSCROLL|WS_TABSTOP);field(L"STATIC",app_.t("memoryEmailNotice"),0,20,355,750,110,SS_LEFT);field(L"BUTTON",app_.t("memoryQuotaLink"),memory_limits,20,490,750,35,WS_TABSTOP);
        }
    }else if(section==3){
        field(L"STATIC",app_.t("offlineLimit"),0,20,20,750,65,SS_LEFT);field(L"BUTTON",app_.t("nativeImport"),pack_import,20,95,350,34,WS_TABSTOP);field(L"BUTTON",app_.t("nativeFolder"),pack_folder,390,95,380,34,WS_TABSTOP);
        field(L"EDIT","https://github.com/sx3111212/breezy-translate/releases/latest/download/ecdict-full-en-zh.json.gz",pack_url,20,148,590,30,ES_AUTOHSCROLL|WS_TABSTOP);field(L"BUTTON",app_.t("nativeDownload"),pack_download,630,148,140,32,WS_TABSTOP);
        auto list=field(L"LISTBOX","",pack_list,20,200,750,245,LBS_NOTIFY|WS_BORDER|WS_VSCROLL|WS_TABSTOP);packs_=app_.dictionary.packs();for(auto& pack:packs_){auto text=pack.name+" · "+(pack.error.empty()?std::to_string(pack.count):pack.error);SendMessageW(list,LB_ADDSTRING,0,(LPARAM)wide(text).c_str());}
        field(L"BUTTON",app_.t("nativeRemove"),pack_delete,20,465,350,34,WS_TABSTOP);field(L"BUTTON",app_.t("nativeReload"),pack_reload,390,465,380,34,WS_TABSTOP);field(L"STATIC",std::to_string(app_.dictionary.count())+" "+app_.t("nativeWords")+" · "+utf8(app_.dictionary.directory().wstring()),pack_info,20,520,750,60,SS_LEFT);
    }else if(section==4){label("interface",20,25);combo(interface_language,22,interfaces_,get(draft_,"InterfaceLanguage","en"));}
    else if(section==5){field(L"STATIC",app_.t("nativeVersion")+": " BREEZY_VERSION "\nWindows x64",0,20,25,750,70);auto button=field(L"BUTTON",app_.t("nativeReleases"),release_page,20,120,750,36,WS_TABSTOP);EnableWindow(button,secure_url(get(app_.links,"ReleasesUrl")));std::string notes;try{notes=read_file(app_dir()/(app_.strings.language.rfind("zh",0)==0?L"CHANGELOG.md":L"CHANGELOG.en.md"),1024*1024);}catch(...){notes="";}field(L"EDIT",notes,0,20,190,750,390,ES_MULTILINE|ES_READONLY|ES_AUTOVSCROLL|WS_VSCROLL);}
    else if(section==6){field(L"STATIC",app_.t("aboutIntro"),0,20,25,750,150,SS_LEFT);field(L"BUTTON","GitHub · Breezy",project,20,200,750,36,WS_TABSTOP);auto button=field(L"BUTTON",app_.t("nativeDonation"),donate,20,260,750,36,WS_TABSTOP);EnableWindow(button,secure_url(get(app_.links,"DonationUrl")));if(!IsWindowEnabled(button))field(L"STATIC",app_.t("nativeNoDonation"),0,20,325,750,70,SS_LEFT);}
    rebuilding_=false;layout();InvalidateRect(host_,nullptr,TRUE);
}
Json SettingsWindow::edited_profile(){
    if(profile_index_<0||profile_index_>=(int)draft_["AiProfiles"].size())throw std::runtime_error("configureAiFirst");auto value=profile_original_;
    value["Name"]=trim(window_text(GetDlgItem(host_,profile_name)));value["BaseUrl"]=trim(window_text(GetDlgItem(host_,address)));value["Model"]=trim(window_text(GetDlgItem(host_,model)));value["Protocol"]=chosen(GetDlgItem(host_,protocol),protocols,"ChatCompletions");
    auto key=window_text(GetDlgItem(host_,api_key));if(!key.empty())value["ProtectedKey"]=protect(key);else if(get(value,"BaseUrl")!=get(profile_original_,"BaseUrl")||get(value,"Protocol")!=get(profile_original_,"Protocol"))value["ProtectedKey"]="";
    if(get(value,"Name").empty())value["Name"]="AI";endpoint(get(value,"BaseUrl"),"models");return value;
}
void SettingsWindow::collect_profile(){if(service_type_!="AI"||profile_index_<0||profile_index_>=(int)draft_["AiProfiles"].size())return;auto value=edited_profile();draft_["AiProfiles"][profile_index_]=value;if(SendMessageW(GetDlgItem(host_,default_profile),BM_GETCHECK,0,0)==BST_CHECKED)draft_["DefaultAiProfileId"]=get(value,"Id");}
void SettingsWindow::load_profile(int index){
    profile_index_=index;profile_original_=index>=0?draft_["AiProfiles"][index]:Json{{"Name",""},{"BaseUrl",""},{"Model",""},{"Protocol","ChatCompletions"},{"ProtectedKey",""}};
    SendMessageW(GetDlgItem(host_,profile_list),CB_SETCURSEL,index,0);set_text(GetDlgItem(host_,profile_name),get(profile_original_,"Name"));set_text(GetDlgItem(host_,address),get(profile_original_,"BaseUrl"));set_text(GetDlgItem(host_,api_key),"");set_text(GetDlgItem(host_,model),get(profile_original_,"Model"));choices(GetDlgItem(host_,protocol),protocols,get(profile_original_,"Protocol","ChatCompletions"));SendMessageW(GetDlgItem(host_,default_profile),BM_SETCHECK,get(draft_,"DefaultAiProfileId")==get(profile_original_,"Id")&&index>=0?BST_CHECKED:BST_UNCHECKED,0);
    int selected=(int)presets().size()-1;for(size_t i=0;i<presets().size();i++)if(presets()[i].url==get(profile_original_,"BaseUrl"))selected=(int)i;SendMessageW(GetDlgItem(host_,preset),CB_SETCURSEL,selected,0);for(int id:{preset,profile_name,address,api_key,model,protocol,default_profile,load_models,test_translation,delete_profile})EnableWindow(GetDlgItem(host_,id),index>=0);
}
void SettingsWindow::collect(){
    if(rebuilding_)return;
    if(section_==0||section_==1){bool writing=section_==1;draft_[writing?"InputTarget":"ReadTarget"]=chosen(GetDlgItem(host_,target_language),languages_,"en");draft_[writing?"InputHotkeyEnabled":"ReadHotkeyEnabled"]=SendMessageW(GetDlgItem(host_,shortcut_enabled),BM_GETCHECK,0,0)==BST_CHECKED;draft_[writing?"InputHotkey":"ReadHotkey"]=window_text(GetDlgItem(host_,shortcut));store_provider(draft_,writing,false,chosen(GetDlgItem(host_,sentence_provider),services_,"MyMemory"));if(writing)draft_["InputSourceLanguage"]=chosen(GetDlgItem(host_,source_language),sources_,"auto");else store_provider(draft_,false,true,chosen(GetDlgItem(host_,word_provider),word_services_,"Offline"));}
    else if(section_==2){if(service_type_=="AI")collect_profile();else if(service_type_=="Azure"){draft_["AzureRegion"]=trim(window_text(GetDlgItem(host_,azure_region)));auto key=window_text(GetDlgItem(host_,azure_key));if(!key.empty())draft_["ProtectedAzureKey"]=protect(key);}else draft_["MyMemoryEmail"]=validate_email(window_text(GetDlgItem(host_,memory_email)));}
    else if(section_==4)draft_["InterfaceLanguage"]=chosen(GetDlgItem(host_,interface_language),interfaces_,"en");
}
void SettingsWindow::command(int id,int notification){
    if(rebuilding_)return;
    if(id==manage){collect();build(2);return;}
    if(id==demo_pause){demo_paused_=!demo_paused_;set_text(GetDlgItem(host_,demo_pause),app_.t(demo_paused_?"playDemo":"pauseDemo"));return;}
    if(id==demo_replay){demo_frame_=0;demo_paused_=false;set_text(GetDlgItem(host_,demo_pause),app_.t("pauseDemo"));InvalidateRect(GetDlgItem(host_,demo),nullptr,FALSE);return;}
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
    if(id==memory_limits){open_url("https://mymemory.translated.net/doc/usagelimits.php");return;}
    if(id==pack_folder){std::filesystem::create_directories(app_.dictionary.directory());ShellExecuteW(window_,L"open",app_.dictionary.directory().c_str(),nullptr,nullptr,SW_SHOWNORMAL);return;}
    if(id==pack_reload){app_.dictionary.reload();build(3);return;}
    if(id==pack_delete){int index=(int)SendMessageW(GetDlgItem(host_,pack_list),LB_GETCURSEL,0,0);if(index>=0&&(size_t)index<packs_.size()){if(MessageBoxW(window_,wide(packs_[index].name+"\n"+app_.t("aiDelete")+"?").c_str(),L"Breezy",MB_YESNO|MB_ICONQUESTION)==IDYES){app_.dictionary.remove(packs_[index].name);build(3);}}return;}
    if(id==pack_import){wchar_t path[32768]{};OPENFILENAMEW dialog{sizeof(dialog)};dialog.hwndOwner=window_;dialog.lpstrFilter=L"Dictionary packs (*.json.gz;*.csv)\0*.json.gz;*.csv\0All files\0*.*\0\0";dialog.lpstrFile=path;dialog.nMaxFile=32768;dialog.Flags=OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST|OFN_NOCHANGEDIR;if(GetOpenFileNameW(&dialog)){Path source(path);auto app=&app_;HWND recipient=window_;show_status(app_.t("nativeImporting"));app_.work([app,recipient,source]{try{app->dictionary.install(source);app->post([app,recipient]{if(app->settings&&app->settings->window()==recipient&&app->settings->section_==3)app->settings->build(3);});}catch(const std::exception& error){auto message=std::string(error.what());app->post([app,recipient,message]{if(app->settings&&app->settings->window()==recipient)app->settings->show_status(message);});}});}return;}
    if(id==pack_download){auto url=trim(window_text(GetDlgItem(host_,pack_url)));if(!secure_url(url))throw std::runtime_error("A direct HTTPS dictionary file URL is required.");auto name=url.substr(url.find_last_of('/')+1);auto query=name.find('?');if(query!=std::string::npos)name.resize(query);if(name.size()<9||name.substr(name.size()-8)!=".json.gz")name="downloaded-pack.json.gz";auto app=&app_;HWND recipient=window_;show_status(app_.t("nativeDownloading"));app_.work([app,recipient,url,name]{try{auto reply=app->http.download(url);if(reply.status!=200)throw std::runtime_error("HTTP "+std::to_string(reply.status));app->dictionary.install_bytes(reply.body,name);app->post([app,recipient]{if(app->settings&&app->settings->window()==recipient&&app->settings->section_==3)app->settings->build(3);});}catch(const std::exception& error){auto message=std::string(error.what());app->post([app,recipient,message]{if(app->settings&&app->settings->window()==recipient)app->settings->show_status(message);});}});return;}
    if(id==release_page){open_url(get(app_.links,"ReleasesUrl"));return;}if(id==donate){open_url(get(app_.links,"DonationUrl"));return;}if(id==project){open_url(get(app_.links,"ProjectUrl"));return;}
}
void SettingsWindow::demo_paint(HWND window){
    HDC dc=GetDC(window);RECT rectangle;GetClientRect(window,&rectangle);HBRUSH background=CreateSolidBrush(RGB(246,248,253));FillRect(dc,&rectangle,background);DeleteObject(background);SetBkMode(dc,TRANSPARENT);SelectObject(dc,app_.font());SetTextColor(dc,RGB(49,63,95));
    auto language=get(draft_,section_==0?"ReadTarget":"InputTarget","en");std::string original="Hi, do you have a moment to chat today?",translated="Hello";for(auto& value:app_.strings.languages())if(value.code==language)translated=value.example;
    unsigned step=(demo_frame_/5)%4;RECT text{24,20,rectangle.right-90,90};DrawTextW(dc,wide(step<2?original:translated).c_str(),-1,&text,DT_WORDBREAK|DT_LEFT);
    if(section_==0&&step<2){HBRUSH selection=CreateSolidBrush(RGB(215,226,255));RECT highlight{22,18,std::min(rectangle.right-100,300L),49};FillRect(dc,&highlight,selection);DeleteObject(selection);DrawTextW(dc,wide(original).c_str(),-1,&text,DT_WORDBREAK|DT_LEFT);}
    if(app_.image_){Gdiplus::Graphics graphics(dc);int x=rectangle.right-90+(step==0?-15:0);graphics.DrawImage(app_.image_.get(),x,24,58,58);}
    RECT instruction{24,112,rectangle.right-24,160};auto key=std::string(section_==0?"selectionStep":"inputStep")+std::to_string(step+1);DrawTextW(dc,wide(app_.t(key)).c_str(),-1,&instruction,DT_WORDBREAK|DT_LEFT);ReleaseDC(window,dc);
}
LRESULT CALLBACK SettingsWindow::hotkey_proc(HWND window,UINT message,WPARAM wparam,LPARAM lparam,UINT_PTR,DWORD_PTR data){auto self=(SettingsWindow*)data;if(message==WM_KEYDOWN||message==WM_SYSKEYDOWN){auto value=record_hotkey((UINT)wparam);if(!value.empty())set_text(window,value);return 0;}if(message==WM_CHAR||message==WM_SYSCHAR)return 0;if(message==WM_NCDESTROY)RemoveWindowSubclass(window,hotkey_proc,1);return DefSubclassProc(window,message,wparam,lparam);}
LRESULT CALLBACK SettingsWindow::host_proc(HWND window,UINT message,WPARAM wparam,LPARAM lparam){auto self=(SettingsWindow*)GetWindowLongPtrW(window,GWLP_USERDATA);if(message==WM_NCCREATE){self=(SettingsWindow*)((CREATESTRUCTW*)lparam)->lpCreateParams;SetWindowLongPtrW(window,GWLP_USERDATA,(LONG_PTR)self);}if(!self)return DefWindowProcW(window,message,wparam,lparam);try{if(message==WM_CTLCOLORSTATIC){SetBkMode((HDC)wparam,TRANSPARENT);SetTextColor((HDC)wparam,RGB(49,63,95));return(LRESULT)GetStockObject(WHITE_BRUSH);}if(message==WM_COMMAND){self->command(LOWORD(wparam),HIWORD(wparam));return 0;}if(message==WM_DRAWITEM&&((DRAWITEMSTRUCT*)lparam)->CtlID==demo){self->demo_paint(((DRAWITEMSTRUCT*)lparam)->hwndItem);return TRUE;}if(message==WM_VSCROLL||message==WM_MOUSEWHEEL){SCROLLINFO scroll{sizeof(scroll),SIF_ALL};GetScrollInfo(window,SB_VERT,&scroll);int previous=scroll.nPos;int code=LOWORD(wparam);if(message==WM_MOUSEWHEEL)scroll.nPos-=(short)HIWORD(wparam)/WHEEL_DELTA*45;else if(code==SB_LINEUP)scroll.nPos-=30;else if(code==SB_LINEDOWN)scroll.nPos+=30;else if(code==SB_PAGEUP)scroll.nPos-=scroll.nPage;else if(code==SB_PAGEDOWN)scroll.nPos+=scroll.nPage;else if(code==SB_THUMBTRACK)scroll.nPos=scroll.nTrackPos;scroll.fMask=SIF_POS;SetScrollInfo(window,SB_VERT,&scroll,TRUE);GetScrollInfo(window,SB_VERT,&scroll);ScrollWindowEx(window,0,previous-scroll.nPos,nullptr,nullptr,nullptr,nullptr,SW_SCROLLCHILDREN|SW_INVALIDATE|SW_ERASE);return 0;}}catch(const std::exception& error){self->show_status(error.what());}return DefWindowProcW(window,message,wparam,lparam);}
LRESULT CALLBACK SettingsWindow::proc(HWND window,UINT message,WPARAM wparam,LPARAM lparam){
    auto self=(SettingsWindow*)GetWindowLongPtrW(window,GWLP_USERDATA);if(message==WM_NCCREATE){self=(SettingsWindow*)((CREATESTRUCTW*)lparam)->lpCreateParams;SetWindowLongPtrW(window,GWLP_USERDATA,(LONG_PTR)self);}if(!self)return DefWindowProcW(window,message,wparam,lparam);
    try{switch(message){
    case WM_CTLCOLORSTATIC:SetBkMode((HDC)wparam,TRANSPARENT);return(LRESULT)GetStockObject(WHITE_BRUSH);
    case WM_GETMINMAXINFO:((MINMAXINFO*)lparam)->ptMinTrackSize={1040,620};return 0;
    case WM_SIZE:self->layout();return 0;
    case WM_COMMAND:if(LOWORD(wparam)==nav&&HIWORD(wparam)==LBN_SELCHANGE&&!self->rebuilding_){int section=(int)SendMessageW(self->navigation_,LB_GETCURSEL,0,0);self->collect();self->build(section);return 0;}else if(LOWORD(wparam)==save){self->collect();self->app_.apply_preferences(self->draft_);DestroyWindow(window);return 0;}else if(LOWORD(wparam)==cancel||LOWORD(wparam)==IDCANCEL){DestroyWindow(window);return 0;}break;
    case WM_TIMER:if((self->section_==0||self->section_==1)&&!self->demo_paused_){self->demo_frame_++;InvalidateRect(GetDlgItem(self->host_,demo),nullptr,FALSE);}return 0;
    case WM_CLOSE:DestroyWindow(window);return 0;
    case WM_DESTROY:self->app_.strings.language=get(self->app_.preferences.snapshot(),"InterfaceLanguage","en");self->app_.hotkeys_->resume();self->window_=nullptr;return 0;
    }}catch(const std::exception& error){self->show_status(error.what());SendMessageW(self->navigation_,LB_SETCURSEL,self->section_,0);}return DefWindowProcW(window,message,wparam,lparam);
}
}

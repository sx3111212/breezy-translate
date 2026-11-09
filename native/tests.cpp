#include "backend.hpp"
#include "automation.hpp"

namespace breezy {
static void require(bool value,const std::string& name){if(!value)throw std::runtime_error("Test failed: "+name);}
template<class F>static void rejected(F callback,const std::string& name){bool failed=false;try{callback();}catch(...){failed=true;}require(failed,name);}
int native_test(){
    Path root=app_dir()/(L"native-test-"+std::to_wstring(GetCurrentProcessId()));
    std::filesystem::create_directories(root);
    try{
        Strings strings;strings.language="en";require(strings.languages().size()==20,"twenty target languages");
        for(auto& language:Strings::interface_languages()){strings.language=language;require(strings.text("settings")!="settings"||language=="en","localized settings");require(strings.text("save")!="save","localized save");}
        strings.language="en";
        require(utf8(wide("Breezy 轻风 日本語 한국어 😀"))=="Breezy 轻风 日本語 한국어 😀","Unicode round trip");
        for(auto& data:std::vector<std::string>{"","hello",std::string(200000,'x'),"中文\nEnglish"}){require(gunzip(gzip_store(data))==data,"gzip round trip");require(gunzip(gzip_compress(data))==data,"compressed gzip round trip");}
        auto corrupt=gzip_store("hello");corrupt.back()^=1;rejected([&]{gunzip(corrupt);},"gzip integrity rejection");
        auto secret=protect("native-test-key");require(secret!="native-test-key"&&unprotect(secret)=="native-test-key","Windows DPAPI round trip");
        require(parse_hotkey("Ctrl+Alt+D").valid&&parse_hotkey("Ctrl+Alt+NumPad7").valid&&!parse_hotkey("D").valid&&!parse_hotkey("Ctrl+F12").valid&&!parse_hotkey("Ctrl+F1trailing").valid,"shortcut validation");
        {
            struct Window{HWND value=CreateWindowExW(0,L"STATIC",L"",0,0,0,0,0,HWND_MESSAGE,nullptr,GetModuleHandleW(nullptr),nullptr);~Window(){UnregisterHotKey(value,71);UnregisterHotKey(value,72);DestroyWindow(value);}} owner,blocker;
            Hotkeys keys(owner.value);auto value=default_preferences();value["ReadHotkey"]="Ctrl+Alt+Shift+F23";value["InputHotkey"]="Ctrl+Alt+Shift+F24";require(keys.apply(value),"register independent native shortcuts");
            UINT modifiers=MOD_CONTROL|MOD_ALT|MOD_SHIFT;require(RegisterHotKey(blocker.value,71,modifiers,VK_F22)!=FALSE,"prepare native shortcut conflict");auto changed=value;changed["ReadHotkey"]="Ctrl+Alt+Shift+F22";require(!keys.apply(changed),"shortcut conflict rejection");require(!RegisterHotKey(blocker.value,72,modifiers,VK_F23),"previous shortcut restored after conflict");keys.suspend();require(RegisterHotKey(blocker.value,72,modifiers,VK_F23)!=FALSE,"shortcuts suspended during settings");UnregisterHotKey(blocker.value,72);keys.resume();require(!RegisterHotKey(blocker.value,72,modifiers,VK_F23),"shortcuts resumed after settings");
        }
        auto defaults=default_preferences();require(flag(defaults,"FollowInput")&&flag(defaults,"FollowSelection"),"both bubble follow modes default on");auto follow=normalize_preferences(Json{{"FollowInput",false},{"FollowSelection",true}});require(!flag(follow,"FollowInput")&&flag(follow,"FollowSelection"),"follow modes normalize independently");require(flag(normalize_preferences(Json::object()),"FollowInput")&&flag(normalize_preferences(Json::object()),"FollowSelection"),"existing settings receive enabled follow defaults");defaults["FutureField"]="preserved";defaults["MyMemoryEmail"]=" tester+native@example.com ";
        auto legacy=normalize_preferences(Json{{"Provider","AI"},{"ReadProvider",""},{"InputProvider",""},{"HotkeysEnabled",false}});require(get(legacy,"ReadProvider")=="AI"&&get(legacy,"InputProvider")=="AI"&&get(legacy,"InputSourceLanguage")=="auto"&&!flag(legacy,"ReadHotkeyEnabled")&&!flag(legacy,"InputHotkeyEnabled"),"legacy service, language, and disabled shortcut migration");
        Preferences preferences(root/L"settings.json");preferences.save(defaults);Preferences restored(root/L"settings.json");
        require(get(restored.snapshot(),"FutureField")=="preserved"&&get(restored.snapshot(),"MyMemoryEmail")=="tester+native@example.com","settings persistence and forward compatibility");
        require(Dictionary::is_word("Generation.")&&Dictionary::is_word("“Generation.”")&&!Dictionary::is_word("a whole sentence")&&!Dictionary::is_word("你好")&&!Dictionary::is_word("hello--world"),"word routing");
        Dictionary dictionary(root/L"Dictionaries");require(dictionary.count()==58222,"embedded dictionary count");
        require(dictionary.lookup("Generation.").find("代")!=std::string::npos,"generation definition");rejected([&]{dictionary.lookup("qzxunknown");},"missing words rejected");
        Json pack={{"qzxrareword",{{"Phonetic",""},{"Translation","罕见词"}}},{"hello",{{"Phonetic",""},{"Translation","覆盖词义"}}}};
        dictionary.install_bytes(gzip_store(pack.dump()),"test.json.gz");require(dictionary.count()==58223&&dictionary.lookup("hello")=="覆盖词义","pack merge");
        rejected([&]{dictionary.install_bytes("bad gzip","test.json.gz");},"invalid pack rejection");require(dictionary.lookup("hello")=="覆盖词义","invalid replacement preserves installed pack");
        rejected([&]{dictionary.remove("../test.json.gz");},"deletion path validation");dictionary.remove("test.json.gz");require(dictionary.count()==58222&&dictionary.lookup("hello")!="覆盖词义","pack removal and fallback");
        auto csv="word,phonetic,translation,bnc,frq,collins,tag\nhello,,你好,1,1,1,\nqzxrareword,,罕见词,0,0,0,\n";
        require(Dictionary::parse_csv(csv).size()==2&&Dictionary::parse_csv(csv,true).size()==1,"full and compact CSV conversion");
        require(validate_email(" test+tag@example.com ")=="test+tag@example.com"&&validate_email("").empty(),"optional email");rejected([&]{validate_email("not-an-email");},"invalid email rejection");
        require(url_encode("test+tag@example.com")=="test%2Btag%40example.com","email URL encoding");
        std::string multilingual;for(int i=0;i<700;i++)multilingual+="中文😀。";std::string combined;for(auto& chunk:chunks(multilingual)){require(chunk.size()<=450,"MyMemory UTF-8 byte limit");wide(chunk);combined+=chunk;}require(combined==multilingual,"multilingual chunking preserves every scalar");
        require(html_decode("&eacute; &#x4E2D; &#128512; &amp;lt;")=="é 中 😀 &lt;","named and numeric HTML decoding without recursive interpretation");
        require(endpoint("https://example.com/v1/chat/completions","models")=="https://example.com/v1/models","endpoint normalization");
        require(secure_url("http://localhost:11434/v1",true)&&!secure_url("http://example.com/v1",true)&&!secure_url("https://user:secret@example.com",true),"transport URL policy");
        rejected([&]{endpoint("https://example.com?key=secret","models");},"query credentials rejected");
        auto settings=default_preferences();settings["AiProfiles"]=Json::array({{{"Id","test"},{"Name","Local test"},{"BaseUrl","http://localhost:11434/v1"},{"Model","test-model"},{"ProtectedKey",""},{"Protocol","ChatCompletions"}}});settings["DefaultAiProfileId"]="test";
        std::vector<std::string> urls;std::vector<Json> bodies;
        Translator translator(dictionary,strings,[&](const std::string& url,const std::string&,const std::string& body,const std::string& headers){
            urls.push_back(url);if(!body.empty())bodies.push_back(Json::parse(body));
            if(url.find("/models")!=std::string::npos)return HttpReply{200,R"({"data":[{"id":"z"},{"id":"a"},{"id":"a"}]})"};
            if(url.find("/chat/completions")!=std::string::npos){require(headers.find("de=")==std::string::npos,"email absent from AI");return HttpReply{200,R"({"choices":[{"message":{"content":"Bonjour"}}]})"};}
            if(url.find("/messages")!=std::string::npos){require(headers.find("anthropic-version:")!=std::string::npos,"Anthropic headers");return HttpReply{200,R"({"content":[{"type":"text","text":"Bonjour"}]})"};}
            if(url.find("cognitive.microsofttranslator.com")!=std::string::npos)return HttpReply{200,R"([{"detectedLanguage":{"language":"en"},"translations":[{"text":"Bonjour"}]}])"};
            return HttpReply{200,R"({"responseStatus":200,"responseData":{"translatedText":"Bonjour &amp; salut"}})"};
        });
        require(translator.translate("generation","en","zh-CN",settings,false,"Offline").service=="ECDICT"&&urls.empty(),"offline lookup makes no network request");
        require(translator.translate("Hello there","en","fr",settings,false,"AI").text=="Bonjour","Chat Completions translation");
        settings["AiProfiles"][0]["Protocol"]="Anthropic";require(translator.translate("Hello there","en","fr",settings,false,"AI").text=="Bonjour","Anthropic translation");
        auto models=translator.models(settings["AiProfiles"][0]);require(models==std::vector<std::string>({"a","z"}),"model enumeration");
        settings["ProtectedAzureKey"]=protect("test-key");require(translator.translate("Hello there","auto","fr",settings,false,"Azure").source=="en","Azure detection and parsing");
        settings["MyMemoryEmail"]="test+native@example.com";
        require(translator.translate("Hello there","en","fr",settings,false,"MyMemory").text=="Bonjour & salut"&&urls.back().find("de=test%2Bnative%40example.com")!=std::string::npos,"MyMemory email encoding and entity decoding");
        auto before=urls.size();translator.translate(std::string(1200,'x'),"en","fr",settings,true,"MyMemory");require(urls.size()-before==3,"long MyMemory requests are segmented");
        Translator denied(dictionary,strings,[](auto&,auto&,auto&,auto&){return HttpReply{401,""};});rejected([&]{denied.translate("Hello there","en","fr",settings,false,"AI");},"provider errors do not fall back");
        write_file(app_dir()/L"native-test.txt","PASS: native Unicode, eight UI languages, twenty targets, gzip integrity, DPAPI, compatible settings, 58,222 embedded words, pack merge/removal, CSV conversion, URL and email validation, model listing, Chat Completions, Anthropic, Azure, MyMemory segmentation, provider isolation. Isolated fixtures; no external requests.\n");
        remove_test_directory(root);return 0;
    }catch(const std::exception& error){write_file(app_dir()/L"native-test.txt",std::string("FAIL: ")+error.what()+"\n");remove_test_directory(root);return 1;}
}
int native_pack_test(const Path& source){
    Path root=app_dir()/(L"native-full-pack-test-"+std::to_wstring(GetCurrentProcessId()));
    try{Dictionary dictionary(root/L"Dictionaries");dictionary.install(source);require(dictionary.count()==399980,"full ECDICT pack count");require(dictionary.packs().size()==1&&dictionary.packs()[0].error.empty(),"full pack status");dictionary.remove(utf8(source.filename().wstring()));require(dictionary.count()==58222,"built-in fallback after full pack removal");write_file(app_dir()/L"native-pack-test.txt","PASS: 399,980 expanded ECDICT entries; loaded pack status; removal and 58,222-word built-in fallback.\n");remove_test_directory(root);return 0;}
    catch(const std::exception& error){write_file(app_dir()/L"native-pack-test.txt",std::string("FAIL: ")+error.what()+"\n");remove_test_directory(root);return 1;}
}
int native_download_test(){
    try{Http http;auto reply=http.download("https://github.com/sx3111212/breezy-translate/releases/latest/download/ECDICT-LICENSE.txt");require(reply.status==200&&reply.body.find("MIT License")!=std::string::npos,"public HTTPS release asset redirects");write_file(app_dir()/L"native-download-test.txt","PASS: public GitHub release download followed HTTPS redirects without authentication.\n");return 0;}
    catch(const std::exception& error){write_file(app_dir()/L"native-download-test.txt",std::string("FAIL: ")+error.what()+"\n");return 1;}
}
}

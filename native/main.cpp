#include "backend.hpp"

namespace breezy {
int native_test();
int run_application(HINSTANCE instance);
int native_ui_test(HINSTANCE instance);
int native_fixture(HINSTANCE instance);
int native_pack_test(const Path& source);
int native_download_test();
}
int WINAPI wWinMain(HINSTANCE instance,HINSTANCE,PWSTR arguments,int) {
    OleInitialize(nullptr);
    int result=0;
    try {
        int count=0;wchar_t** items=CommandLineToArgvW(GetCommandLineW(),&count);std::vector<std::wstring> args;for(int i=1;i<count;i++)args.emplace_back(items[i]);LocalFree(items);
        if(args.size()==3&&(args[0]==L"--build-dictionary"||args[0]==L"--build-full-dictionary")){auto entries=breezy::Dictionary::parse_csv(breezy::read_file(args[1]),args[0]==L"--build-dictionary");breezy::write_file(args[2],breezy::gzip_compress(entries.dump()));breezy::write_file(breezy::app_dir()/L"dictionary-build.txt","Built "+std::to_string(entries.size())+" dictionary entries.\n");}
        else if(args.size()==2&&args[0]==L"--install-dictionary"){breezy::Dictionary dictionary;dictionary.install(args[1]);breezy::write_file(breezy::app_dir()/L"dictionary-install.txt","Installed dictionary: "+std::to_string(dictionary.count())+" total entries.\n");}
        else if(args.size()==1&&args[0]==L"--native-download-test")result=breezy::native_download_test();
        else if(args.size()==2&&args[0]==L"--native-pack-test")result=breezy::native_pack_test(args[1]);
        else if(std::wstring(arguments).find(L"--native-ui-test")!=std::wstring::npos)result=breezy::native_ui_test(instance);
        else if(std::wstring(arguments).find(L"--native-fixture")!=std::wstring::npos)result=breezy::native_fixture(instance);
        else if(std::wstring(arguments).find(L"--native-test")!=std::wstring::npos)result=breezy::native_test();
        else result=breezy::run_application(instance);
    }catch(const std::exception& error){MessageBoxW(nullptr,breezy::wide(error.what()).c_str(),L"Breezy",MB_OK|MB_ICONERROR);result=1;}
    OleUninitialize();return result;
}

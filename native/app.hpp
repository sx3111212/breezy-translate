#pragma once
#include "backend.hpp"
#include "automation.hpp"
#include <gdiplus.h>
#include <sapi.h>

namespace breezy {
class SettingsWindow;
class ResultWindow;
class App {
    friend class SettingsWindow;
    HINSTANCE instance_;
    HWND main_=nullptr,bubble_=nullptr;
    HWND tooltip_=nullptr;
    std::wstring tooltip_text_;
    HHOOK mouse_=nullptr;
    UINT taskbar_created_=0;
    HFONT font_=nullptr,title_font_=nullptr;
    ULONG_PTR graphics_=0;
    std::unique_ptr<Gdiplus::Bitmap> image_;
    NOTIFYICONDATAW tray_{};
    std::unique_ptr<Hotkeys> hotkeys_;
    std::unique_ptr<Worker> accessibility_,network_;
    std::atomic<bool> polling_{false},closing_{false};
    bool hover_=false,dragging_=false,pinned_=false,bottom_=false,busy_=false;
    POINT drag_start_{},bubble_start_{},released_{};
    Anchor anchor_;
    HWND last_external_=nullptr;
    std::atomic<unsigned> operation_{0};
    DWORD success_until_=0;
    Com<ISpVoice> voice_;
    std::string voice_language_;
    static App* active_;
    static LRESULT CALLBACK main_proc(HWND,UINT,WPARAM,LPARAM);
    static LRESULT CALLBACK bubble_proc(HWND,UINT,WPARAM,LPARAM);
    static LRESULT CALLBACK mouse_proc(int,WPARAM,LPARAM);
    void render_bubble();
    void position_bubble(const Anchor& anchor);
    void poll();
    void install_tray();
    void menu(POINT point);
public:
    Strings strings;
    Preferences preferences;
    Dictionary dictionary;
    Http http;
    std::function<void(const std::string&,const std::string&,bool)> speech_output;
    Json links;
    std::unique_ptr<SettingsWindow> settings;
    std::unique_ptr<ResultWindow> result;
    explicit App(HINSTANCE instance,Path preferences_path=data_dir()/L"settings.json",Path dictionary_path=data_dir()/L"Dictionaries");
    ~App();
    int run();
    HWND window() const{return main_;}
    HINSTANCE instance() const{return instance_;}
    HFONT font(bool title=false)const{return title?title_font_:font_;}
    std::string t(const std::string& key)const{return strings.text(key);}
    void post(std::function<void()> callback);
    void work(std::function<void()> callback);
    void translate(bool writing,const std::string& manual="",const std::string& source="",const std::string& target="",const std::string& provider="",const std::string& model="");
    void retranslate(const std::string& text,const std::string& source,const std::string& target,const std::string& provider,const std::string& model="",bool writing=false);
    void open_settings(int section=0);
    void open_manual();
    void error(const std::string& message,HWND owner=nullptr);
    void speak(const std::string& text,bool stop=false,const std::string& language="en");
    void replace(const Target& target,const std::string& text);
    void apply_preferences(const Json& value);
    void cancel_translation();
    void dismiss_result();
};

struct Choice { std::string code,label; };
HWND control(App& app,HWND parent,const wchar_t* type,const std::string& text,int id,int x,int y,int width,int height,DWORD style=0);
std::vector<Choice> language_choices(const App& app,bool source=false,bool interface_only=false);
std::vector<Choice> service_choices(const App& app,const Json& settings,bool word=false);
void choices(HWND combo,const std::vector<Choice>& items,const std::string& selected);
std::string chosen(HWND combo,const std::vector<Choice>& items,const std::string& fallback="");
void round_window(HWND window);

class ResultWindow {
    App& app_;
    HWND window_=nullptr,source_=nullptr,target_=nullptr,source_language_=nullptr,target_language_=nullptr,service_=nullptr,status_=nullptr,auto_read_=nullptr;
    HWND model_=nullptr;
    bool compact_=false,manual_=false,receiving_=false;
    unsigned generation_=0;
    std::vector<Choice> sources_,targets_,services_;
    Target original_;
    std::string source_code_="auto",target_code_,provider_;
    std::string ui_language_;
    static LRESULT CALLBACK proc(HWND,UINT,WPARAM,LPARAM);
    void create(bool compact);
    void resize();
    void speak();
public:
    explicit ResultWindow(App& app):app_(app){}
    ~ResultWindow();
    void show(const Target& original,const Translation& translated,const std::string& provider);
    void manual();
    void show_error(const std::string& message);
    void pending();
    HWND window()const{return window_;}
    HWND original_window()const{return original_.foreground;}
    void hide();
};

class SettingsWindow {
    App& app_;
    HWND window_=nullptr,navigation_=nullptr,host_=nullptr,title_=nullptr,status_=nullptr;
    Json draft_,profile_original_;
    int section_=0,profile_index_=-1;
    std::vector<HWND> fields_;
    std::vector<Choice> languages_,sources_,interfaces_,services_,word_services_;
    std::vector<Pack> packs_;
    std::vector<std::string> model_names_;
    bool rebuilding_=false,dirty_profile_=false,demo_paused_=false;
    std::string service_type_="AI";
    unsigned demo_frame_=0;
    void layout();
    static LRESULT CALLBACK proc(HWND,UINT,WPARAM,LPARAM);
    static LRESULT CALLBACK host_proc(HWND,UINT,WPARAM,LPARAM);
    static LRESULT CALLBACK hotkey_proc(HWND,UINT,WPARAM,LPARAM,UINT_PTR,DWORD_PTR);
    void build(int section);
    void collect();
    void collect_profile();
    void load_profile(int index);
    Json edited_profile();
    HWND field(const wchar_t* type,const std::string& text,int id,int x,int y,int width,int height,DWORD style=0);
    void label(const std::string& key,int x,int y,int width=180,int height=28);
    void refresh_language();
    void command(int id,int notification);
    void demo_paint(HWND window);
public:
    SettingsWindow(App& app,int section);
    ~SettingsWindow();
    HWND window()const{return window_;}
    void focus(int section);
    void show_status(const std::string& message);
};
}

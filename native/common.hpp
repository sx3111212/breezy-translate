#pragma once
#ifndef NOMINMAX
#define NOMINMAX
#endif
#ifndef UNICODE
#define UNICODE
#define _UNICODE
#endif
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>
#include <commctrl.h>
#include <shellapi.h>
#include <shlobj.h>
#include <wincrypt.h>
#include <winhttp.h>
#include <objbase.h>
#include <oleauto.h>
#include <string>
#include <vector>
#include <map>
#include <set>
#include <memory>
#include <functional>
#include <mutex>
#include <atomic>
#include <thread>
#include <condition_variable>
#include <queue>
#include <fstream>
#include <filesystem>
#include <algorithm>
#include <stdexcept>
#include <sstream>
#include <chrono>
#include <future>
#include "vendor/json.hpp"
#include "version.hpp"

namespace breezy {
using Json = nlohmann::json;
using Path = std::filesystem::path;
using namespace std::chrono_literals;
std::string utf8(const std::wstring& value);
std::wstring wide(const std::string& value);
std::string trim(std::string value);
std::string lower(std::string value);
std::string get(const Json& object, const char* key, const std::string& fallback = "");
bool flag(const Json& object, const char* key, bool fallback = false);
std::string read_file(const Path& path, size_t limit = 128 * 1024 * 1024);
void write_file(const Path& path, const std::string& bytes);
void atomic_write(const Path& path, const std::string& bytes);
void remove_test_directory(const Path& path);
Path app_dir();
Path data_dir();
std::string resource(int id);
uint32_t crc32(const std::string& value);
std::string gunzip(const std::string& value);
std::string gzip_store(const std::string& value);
std::string gzip_compress(const std::string& value);
std::string protect(const std::string& plain);
std::string unprotect(const std::string& cipher);
std::string url_encode(const std::string& value);
std::string html_decode(std::string value);
bool secure_url(const std::string& value, bool allow_loopback = false);
void open_url(const std::string& value);
std::string window_text(HWND window);
void set_text(HWND window, const std::string& value);

template<class T> class Com {
    T* value_ = nullptr;
public:
    Com() = default;
    explicit Com(T* value): value_(value) {}
    Com(const Com& other): value_(other.value_) { if (value_) value_->AddRef(); }
    Com(Com&& other) noexcept: value_(other.value_) { other.value_ = nullptr; }
    ~Com() { if (value_) value_->Release(); }
    Com& operator=(Com other) noexcept { std::swap(value_, other.value_); return *this; }
    T* get() const { return value_; }
    T* operator->() const { return value_; }
    explicit operator bool() const { return value_ != nullptr; }
    T** put() { if (value_) value_->Release(); value_ = nullptr; return &value_; }
};

struct Language { std::string code, name, hello, example; };
class Strings {
    Json entries_;
    std::vector<Language> languages_;
public:
    std::string language = "en";
    Strings();
    std::string text(const std::string& key) const;
    const std::vector<Language>& languages() const { return languages_; }
    std::string language_name(const std::string& code) const;
    bool supported(const std::string& code) const;
    static const std::vector<std::string>& interface_languages();
    static std::string system_language();
    static std::string system_target_language();
    static std::string guess(const std::string& text);
};

// A bounded serial worker avoids touching UI Automation inside a mouse hook or UI callback.
class Worker {
    std::mutex mutex_;
    std::condition_variable ready_;
    std::queue<std::function<void()>> pending_;
    bool stopping_ = false;
    std::thread thread_;
public:
    Worker();
    ~Worker();
    void post(std::function<void()> task);
};
}

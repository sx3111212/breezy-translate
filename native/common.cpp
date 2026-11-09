#include "common.hpp"
#include "vendor/miniz_tinfl.h"
#include "vendor/miniz_tdef.h"
#include <cctype>

namespace breezy {
std::string utf8(const std::wstring& value) {
    if (value.empty()) return {};
    int count = WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, value.data(), (int)value.size(), nullptr, 0, nullptr, nullptr);
    if (!count) throw std::runtime_error("Invalid Unicode text.");
    std::string result(count, 0);
    WideCharToMultiByte(CP_UTF8, WC_ERR_INVALID_CHARS, value.data(), (int)value.size(), result.data(), count, nullptr, nullptr);
    return result;
}
std::wstring wide(const std::string& value) {
    if (value.empty()) return {};
    int count = MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, value.data(), (int)value.size(), nullptr, 0);
    if (!count) throw std::runtime_error("Invalid UTF-8 text.");
    std::wstring result(count, 0);
    MultiByteToWideChar(CP_UTF8, MB_ERR_INVALID_CHARS, value.data(), (int)value.size(), result.data(), count);
    return result;
}
std::string trim(std::string value) {
    auto whitespace = [](unsigned char c) { return std::isspace(c) != 0; };
    value.erase(value.begin(), std::find_if_not(value.begin(), value.end(), whitespace));
    value.erase(std::find_if_not(value.rbegin(), value.rend(), whitespace).base(), value.end());
    return value;
}
std::string lower(std::string value) { for (auto& c : value) if ((unsigned char)c < 128) c = (char)std::tolower((unsigned char)c); return value; }
std::string get(const Json& object, const char* key, const std::string& fallback) {
    auto found = object.find(key);
    return found != object.end() && found->is_string() ? found->get<std::string>() : fallback;
}
bool flag(const Json& object, const char* key, bool fallback) {
    auto found = object.find(key);
    return found != object.end() && found->is_boolean() ? found->get<bool>() : fallback;
}
std::string read_file(const Path& path, size_t limit) {
    std::ifstream stream(path, std::ios::binary);
    if (!stream) throw std::runtime_error("Cannot read file: " + utf8(path.filename().wstring()));
    stream.seekg(0, std::ios::end);
    auto size = stream.tellg();
    if (size < 0 || (uint64_t)size > limit) throw std::runtime_error("File exceeds the supported size limit.");
    std::string result((size_t)size, 0);
    stream.seekg(0); if (!result.empty()) stream.read(result.data(), result.size());
    if (!stream && !result.empty()) throw std::runtime_error("Cannot read complete file.");
    return result;
}
void write_file(const Path& path, const std::string& bytes) {
    std::ofstream stream(path, std::ios::binary | std::ios::trunc);
    stream.write(bytes.data(), bytes.size()); stream.close();
    if (!stream) throw std::runtime_error("Cannot write file.");
}
void atomic_write(const Path& path, const std::string& bytes) {
    std::filesystem::create_directories(path.parent_path());
    auto temporary = path; temporary += L".tmp";
    write_file(temporary, bytes);
    if (!MoveFileExW(temporary.c_str(), path.c_str(), MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH)) {
        DeleteFileW(temporary.c_str()); throw std::runtime_error("Cannot save settings or dictionary; the previous file was preserved.");
    }
}
void remove_test_directory(const Path& path){auto absolute=std::filesystem::absolute(path).lexically_normal();auto parent=std::filesystem::absolute(app_dir()).lexically_normal();auto name=utf8(absolute.filename().wstring());if(absolute.parent_path()!=parent||name.rfind("native-",0)!=0||std::filesystem::is_symlink(absolute))throw std::runtime_error("Invalid test cleanup path.");std::filesystem::remove_all(absolute);}
Path app_dir() { wchar_t buffer[32768]; DWORD size = GetModuleFileNameW(nullptr, buffer, 32768); return Path(std::wstring(buffer, size)).parent_path(); }
Path data_dir() {
    wchar_t* value = nullptr;
    if (FAILED(SHGetKnownFolderPath(FOLDERID_LocalAppData, 0, nullptr, &value))) throw std::runtime_error("Cannot locate local application data.");
    Path result = Path(value) / L"EnglishHelper"; CoTaskMemFree(value); return result;
}
std::string resource(int id) {
    HRSRC item = FindResourceW(nullptr, MAKEINTRESOURCEW(id), RT_RCDATA);
    if (!item) throw std::runtime_error("Missing embedded resource.");
    auto handle = LoadResource(nullptr, item);
    return std::string((const char*)LockResource(handle), SizeofResource(nullptr, item));
}
uint32_t crc32(const std::string& value) {
    static const auto table = [] { std::vector<uint32_t> data(256); for (unsigned i = 0; i < 256; i++) { uint32_t c = i; for (int n = 0; n < 8; n++) c = (c >> 1) ^ (0xEDB88320u & (0u - (c & 1))); data[i] = c; } return data; }();
    uint32_t crc = ~0u;
    for (unsigned char c : value) crc = table[(crc ^ c) & 255] ^ (crc >> 8);
    return ~crc;
}
static uint32_t little32(const std::string& value, size_t offset) {
    uint32_t result = 0; for (int i = 0; i < 4; i++) result |= (uint32_t)(unsigned char)value.at(offset + i) << (8 * i); return result;
}
std::string gunzip(const std::string& value) {
    if (value.size() < 18 || (unsigned char)value[0] != 31 || (unsigned char)value[1] != 139 || value[2] != 8 || ((unsigned char)value[3] & 0xE0)) throw std::runtime_error("Invalid gzip dictionary.");
    unsigned flags = (unsigned char)value[3]; size_t start = 10, end = value.size() - 8;
    if (flags & 4) { if (start + 2 > end) throw std::runtime_error("Invalid gzip header."); size_t length = (unsigned char)value[start] | ((unsigned char)value[start + 1] << 8); start += 2 + length; }
    for (unsigned bit : {8u, 16u}) if (flags & bit) { while (start < end && value[start]) start++; start++; }
    if (flags & 2) { if (start + 2 > end || (crc32(value.substr(0, start)) & 65535) != ((unsigned char)value[start] | ((unsigned char)value[start + 1] << 8))) throw std::runtime_error("Invalid gzip header checksum."); start += 2; }
    uint32_t length = little32(value, value.size() - 4);
    if (start >= end || length > 128 * 1024 * 1024) throw std::runtime_error("Invalid or oversized gzip dictionary.");
    std::string result(std::max<uint32_t>(length, 1), 0);
    size_t count = tinfl_decompress_mem_to_mem(result.data(), result.size(), value.data() + start, end - start, 0);
    if (count != length) throw std::runtime_error("Dictionary decompression failed.");
    result.resize(length);
    if (crc32(result) != little32(value, value.size() - 8)) throw std::runtime_error("Dictionary checksum mismatch.");
    return result;
}
std::string gzip_store(const std::string& value) {
    std::string result("\x1f\x8b\x08\0\0\0\0\0\0\xff", 10);
    size_t position = 0;
    do {
        unsigned length = (unsigned)std::min<size_t>(65535, value.size() - position);
        result.push_back(position + length == value.size() ? 1 : 0);
        result.push_back((char)length); result.push_back((char)(length >> 8));
        result.push_back((char)~length); result.push_back((char)(~length >> 8));
        result.append(value, position, length); position += length;
    } while (position < value.size());
    for (uint32_t number : {crc32(value), (uint32_t)value.size()}) for (int i = 0; i < 4; i++) result.push_back((char)(number >> (8 * i)));
    return result;
}
std::string gzip_compress(const std::string& value){
    size_t length=0;void* compressed=tdefl_compress_mem_to_heap(value.data(),value.size(),&length,TDEFL_DEFAULT_MAX_PROBES);
    if(!compressed)throw std::runtime_error("Dictionary compression failed.");
    std::string result("\x1f\x8b\x08\0\0\0\0\0\0\xff",10);result.append((const char*)compressed,length);free(compressed);
    for(uint32_t number:{crc32(value),(uint32_t)value.size()})for(int i=0;i<4;i++)result.push_back((char)(number>>(8*i)));return result;
}
static std::string base64(const std::string& bytes) {
    DWORD count = 0; CryptBinaryToStringA((const BYTE*)bytes.data(), (DWORD)bytes.size(), CRYPT_STRING_BASE64 | CRYPT_STRING_NOCRLF, nullptr, &count);
    std::string result(count, 0);
    if (!CryptBinaryToStringA((const BYTE*)bytes.data(), (DWORD)bytes.size(), CRYPT_STRING_BASE64 | CRYPT_STRING_NOCRLF, result.data(), &count)) throw std::runtime_error("Cannot encode protected key.");
    while (!result.empty() && result.back() == 0) result.pop_back(); return result;
}
static std::string decode64(const std::string& bytes) {
    DWORD count = 0;
    if (!CryptStringToBinaryA(bytes.data(), (DWORD)bytes.size(), CRYPT_STRING_BASE64, nullptr, &count, nullptr, nullptr)) throw std::runtime_error("Invalid protected key.");
    std::string result(count, 0);
    if (!CryptStringToBinaryA(bytes.data(), (DWORD)bytes.size(), CRYPT_STRING_BASE64, (BYTE*)result.data(), &count, nullptr, nullptr)) throw std::runtime_error("Invalid protected key.");
    result.resize(count); return result;
}
static std::string crypt(const std::string& input, bool encrypt) {
    std::string copy = input;
    DATA_BLOB in{(DWORD)copy.size(), (BYTE*)copy.data()}, out{};
    BOOL success = encrypt ? CryptProtectData(&in, L"Language Helper API key", nullptr, nullptr, nullptr, CRYPTPROTECT_UI_FORBIDDEN, &out) : CryptUnprotectData(&in, nullptr, nullptr, nullptr, nullptr, CRYPTPROTECT_UI_FORBIDDEN, &out);
    SecureZeroMemory(copy.data(), copy.size());
    if (!success) throw std::runtime_error("Cannot access this Windows user's saved API key. Re-enter the key in Settings.");
    std::string result((char*)out.pbData, out.cbData); SecureZeroMemory(out.pbData, out.cbData); LocalFree(out.pbData); return result;
}
std::string protect(const std::string& plain) { return plain.empty() ? "" : base64(crypt(plain, true)); }
std::string unprotect(const std::string& cipher) { return cipher.empty() ? "" : crypt(decode64(cipher), false); }
std::string url_encode(const std::string& value) {
    static const char* hex = "0123456789ABCDEF"; std::string result;
    for (unsigned char c : value) if (std::isalnum(c) && c < 128 || c == '-' || c == '_' || c == '.' || c == '~') result += (char)c;
    else { result += '%'; result += hex[c >> 4]; result += hex[c & 15]; }
    return result;
}
std::string html_decode(std::string value) {
    static const Json entities=Json::parse(resource(14));std::string result;
    for(size_t position=0;position<value.size();position++){
        if(value[position]=='&'){auto end=value.find(';',position+1);if(end!=std::string::npos&&end-position<34){auto name=value.substr(position+1,end-position-1);uint32_t scalar=0;bool recognized=false;
            if(!name.empty()&&name[0]=='#'){try{bool hex=name.size()>2&&(name[1]=='x'||name[1]=='X');size_t count=0;auto number=name.substr(hex?2:1);scalar=(uint32_t)std::stoul(number,&count,hex?16:10);recognized=count==number.size()&&scalar>0&&scalar<=0x10ffff&&!(scalar>=0xd800&&scalar<=0xdfff);}catch(...){}}
            else if(entities.contains(name)){scalar=entities[name].get<uint32_t>();recognized=true;}
            if(recognized){std::wstring text;if(scalar<=65535)text+=(wchar_t)scalar;else{scalar-=0x10000;text+=(wchar_t)(0xd800+(scalar>>10));text+=(wchar_t)(0xdc00+(scalar&1023));}result+=utf8(text);position=end;continue;}
        }}result+=value[position];
    }return result;
}
bool secure_url(const std::string& value, bool allow_loopback) {
    auto url = wide(value); URL_COMPONENTS info{}; info.dwStructSize = sizeof(info); info.dwHostNameLength = info.dwUserNameLength = info.dwPasswordLength = 1;
    if (!WinHttpCrackUrl(url.c_str(), (DWORD)url.size(), 0, &info) || info.dwUserNameLength || info.dwPasswordLength) return false;
    auto host = lower(utf8(std::wstring(info.lpszHostName, info.dwHostNameLength)));
    return info.nScheme == INTERNET_SCHEME_HTTPS || allow_loopback && info.nScheme == INTERNET_SCHEME_HTTP && (host == "localhost" || host == "127.0.0.1" || host == "[::1]" || host == "::1");
}
void open_url(const std::string& value) { if (!secure_url(value)) throw std::runtime_error("A valid HTTPS address has not been configured."); ShellExecuteW(nullptr, L"open", wide(value).c_str(), nullptr, nullptr, SW_SHOWNORMAL); }
std::string window_text(HWND window) { int length = GetWindowTextLengthW(window); std::wstring text(length + 1, 0); int count = GetWindowTextW(window, text.data(), length + 1); text.resize(count); return utf8(text); }
void set_text(HWND window, const std::string& value) { SetWindowTextW(window, wide(value).c_str()); }

const std::vector<std::string>& Strings::interface_languages() { static std::vector<std::string> list{"zh-CN","zh-TW","en","ja","ko","fr","de","es"}; return list; }
Strings::Strings() {
    entries_ = Json::parse(resource(12));
    for (auto& item : Json::parse(resource(13))) languages_.push_back({item.at(0),item.at(1),item.at(2),item.at(3)});
    language = system_language();
}
std::string Strings::text(const std::string& key) const {
    auto found = entries_.find(key); if (found == entries_.end() || !found->is_array()) return key;
    auto& codes = interface_languages(); auto position = std::find(codes.begin(), codes.end(), language); size_t index = position == codes.end() ? 2 : (size_t)(position - codes.begin());
    return found->at(std::min(index, found->size() - 1)).get<std::string>();
}
std::string Strings::language_name(const std::string& code) const { for (auto& item : languages_) if (item.code == code) return item.name; return code; }
bool Strings::supported(const std::string& code) const { return std::any_of(languages_.begin(), languages_.end(), [&](auto& item) { return item.code == code; }); }
std::string Strings::system_language() {
    auto language=system_target_language();for(auto& item:interface_languages())if(item==language)return language;return "en";
}
std::string Strings::system_target_language() {
    wchar_t name[LOCALE_NAME_MAX_LENGTH]{};if(!LCIDToLocaleName(MAKELCID(GetUserDefaultUILanguage(),SORT_DEFAULT),name,LOCALE_NAME_MAX_LENGTH,0))GetUserDefaultLocaleName(name, LOCALE_NAME_MAX_LENGTH);auto code = lower(utf8(name));
    if (code.rfind("zh",0) == 0) return code.find("tw") != std::string::npos || code.find("hk") != std::string::npos || code.find("hant") != std::string::npos ? "zh-TW" : "zh-CN";
    code = code.substr(0, code.find('-'));
    for (const auto& item : Json::parse(resource(13))) if (item.at(0).get<std::string>() == code) return code;
    return "en";
}
std::string Strings::guess(const std::string& text) {
    auto value = wide(text);
    for (wchar_t c : value) if (c >= 0x3040 && c <= 0x30ff) return "ja";
    for (wchar_t c : value) if (c >= 0xac00 && c <= 0xd7af) return "ko";
    for (wchar_t c : value) if (c >= 0x3400 && c <= 0x9fff) return "zh-CN";
    for (wchar_t c : value) if (std::wstring(L"іїєґІЇЄҐ").find(c) != std::wstring::npos) return "uk";
    for (wchar_t c : value) { if (c >= 0x400 && c <= 0x4ff) return "ru"; if (c >= 0x600 && c <= 0x6ff) return "ar"; if (c >= 0x900 && c <= 0x97f) return "hi"; if (c >= 0xe00 && c <= 0xe7f) return "th"; }
    return "en";
}
Worker::Worker(): thread_([this] {
    CoInitializeEx(nullptr, COINIT_MULTITHREADED);
    for (;;) {
        std::function<void()> next;
        { std::unique_lock<std::mutex> lock(mutex_); ready_.wait(lock, [&] { return stopping_ || !pending_.empty(); }); if (stopping_) break; next = std::move(pending_.front()); pending_.pop(); }
        try { next(); } catch (...) { /* Individual jobs report their errors to the UI. */ }
    }
    CoUninitialize();
}) {}
Worker::~Worker() { { std::lock_guard<std::mutex> lock(mutex_); stopping_ = true; } ready_.notify_all(); if (thread_.joinable()) thread_.join(); }
void Worker::post(std::function<void()> task) { { std::lock_guard<std::mutex> lock(mutex_); if (stopping_) return; pending_.push(std::move(task)); } ready_.notify_one(); }
}

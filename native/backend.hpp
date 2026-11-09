#pragma once
#include "common.hpp"

namespace breezy {
Json default_preferences();
Json normalize_preferences(Json value);
class Preferences {
    Path path_;
    mutable std::mutex mutex_;
    Json value_;
public:
    explicit Preferences(Path path = data_dir() / L"settings.json");
    Json snapshot() const;
    void save(Json value);
};

struct Pack { std::string name; size_t count = 0; std::string error; };
class Dictionary {
    Path directory_;
    std::mutex mutex_;
    bool loaded_ = false;
    Json entries_;
    std::vector<Pack> packs_;
    void load();
public:
    explicit Dictionary(Path folder = data_dir() / L"Dictionaries"): directory_(std::move(folder)) {}
    static std::string normalize(std::string text);
    static bool is_word(const std::string& text);
    static Json parse_pack(const std::string& bytes);
    static Json parse_csv(const std::string& text, bool compact = false);
    std::string lookup(const std::string& text);
    size_t count();
    std::vector<Pack> packs();
    void reload();
    void install(const Path& source);
    void install_bytes(const std::string& bytes, const std::string& filename);
    void remove(const std::string& filename);
    const Path& directory() const { return directory_; }
};

struct HttpReply { unsigned status = 0; std::string body; std::string location; };
using Transport = std::function<HttpReply(const std::string&, const std::string&, const std::string&, const std::string&)>;
class Http {
    std::mutex mutex_;
    HINTERNET request_ = nullptr;
public:
    HttpReply send(const std::string& url, const std::string& method = "GET", const std::string& body = "", const std::string& headers = "");
    HttpReply download(std::string url);
    void cancel();
};
struct Translation { std::string text, source, target, service, model; };
struct Preset { std::string name, url, protocol; };
const std::vector<Preset>& presets();
std::string endpoint(std::string address, const std::string& resource);
std::string validate_email(const std::string& email);
std::vector<std::string> chunks(const std::string& text);
Json find_profile(const Json& preferences, const std::string& id);
std::string resolve_provider(const Json& preferences, bool writing, bool word);
std::string resolve_profile(const Json& preferences, bool writing, bool word);
class Translator {
    Dictionary& dictionary_;
    Strings& strings_;
    Transport transport_;
    HttpReply request(const std::string& url, const std::string& method = "GET", const std::string& body = "", const std::string& headers = "");
public:
    Translator(Dictionary& dictionary, Strings& strings, Transport transport): dictionary_(dictionary), strings_(strings), transport_(std::move(transport)) {}
    Translation translate(const std::string& text, std::string source, const std::string& target, const Json& settings, bool writing = false, std::string provider = "", std::string profile_id = "");
    std::vector<std::string> models(const Json& profile);
};
}

namespace EnglishHelper;

static class UiText
{
    public static readonly string[] InterfaceLanguages = ["zh-CN", "zh-TW", "en", "ja", "ko", "fr", "de", "es"];
    public static bool Supports(string code) => InterfaceLanguages.Contains(code);
    public static string SystemLanguage => Supports(Languages.SystemLanguage) ? Languages.SystemLanguage : "en";
    static readonly Dictionary<string, string> Values = new()
    {
        ["memoryIntro"] = "MyMemory 是第三方在线翻译平台，无需注册或填写 API 密钥。|MyMemory 是第三方線上翻譯平台，無需註冊或 API 金鑰。|MyMemory is a third-party online translation service. No registration or API key is required.|MyMemory は登録や API キー不要の外部翻訳サービスです。|MyMemory는 가입이나 API 키 없이 쓰는 외부 번역 서비스입니다.|MyMemory est un service de traduction tiers, sans inscription ni clé API.|MyMemory ist ein externer Übersetzungsdienst ohne Registrierung oder API-Schlüssel.|MyMemory es un servicio externo sin registro ni clave API.",
        ["memoryQuota"] = "匿名免费额度：每天 5,000 字符。\n按平台当前规则，提供有效联系邮箱可提高到每天 50,000 字符。|匿名免費額度：每天 5,000 字元。\n依平台目前規則，提供有效聯絡信箱可提高到每天 50,000 字元。|Anonymous allowance: 5,000 characters/day.\nUnder the provider’s current rules, a valid contact email raises it to 50,000 characters/day.|匿名利用は 1 日 5,000 文字。現在の規定では有効な連絡先メールを提供すると 1 日 50,000 文字です。|익명 한도: 하루 5,000자. 현재 정책상 유효한 연락 이메일을 제공하면 하루 50,000자로 늘어납니다.|Quota anonyme : 5 000 caractères/jour. Selon les règles actuelles, une adresse valide porte ce quota à 50 000.|Anonym: 5.000 Zeichen/Tag. Nach aktuellen Regeln erhöht eine gültige Kontaktadresse das Kontingent auf 50.000.|Cuota anónima: 5.000 caracteres/día. Según las reglas actuales, un correo válido la eleva a 50.000.",
        ["contactEmail"] = "联系邮箱（选填）|聯絡信箱（選填）|Contact email (optional)|連絡先メール（任意）|연락 이메일 (선택)|E-mail (facultatif)|Kontakt-E-Mail (optional)|Correo (opcional)",
        ["clearEmail"] = "清除邮箱|清除信箱|Clear email|メールを消去|이메일 지우기|Effacer|Löschen|Borrar correo",
        ["memoryEmailNotice"] = "留空可匿名使用。保存后，邮箱会随 MyMemory 翻译请求发送给平台，用于异常联系。\n邮箱保存在本机，不经过开发者服务器。额度以平台规则为准，不保证每人独享。|留空可繼續匿名使用。儲存後，MyMemory 翻譯請求會附帶信箱，供平台在異常時聯絡。\n信箱存於本機，不經開發者伺服器。實際額度依平台規則，不保證每人獨享。|Leave blank for anonymous use. Saving an email includes it in MyMemory translation requests so the provider can contact you about problems.\nStored locally; no developer server. Quotas follow provider rules and are not guaranteed per person.|空欄なら匿名利用。保存したメールは MyMemory 翻訳リクエストに付けて送信され、問題時の連絡に使われます。端末に保存し、開発者サーバーは経由しません。利用枠は提供元の規定に従います。|비워두면 익명 사용합니다. 저장한 이메일은 MyMemory 번역 요청에 포함되며 문제 발생 시 연락용으로 사용됩니다. 로컬에 저장되며 개발자 서버를 거치지 않습니다. 개인별 한도는 보장되지 않습니다.|Laissez vide pour rester anonyme. L’adresse enregistrée accompagne les requêtes MyMemory pour être contacté en cas de problème. Stockage local, sans serveur du développeur. Quotas selon le fournisseur, sans garantie individuelle.|Leer lassen für anonyme Nutzung. Gespeicherte E-Mails werden bei MyMemory-Anfragen zur Kontaktaufnahme mitgesendet. Lokal gespeichert, ohne Entwicklerserver. Kontingente nach Anbieterregeln, keine persönliche Garantie.|Déjalo vacío para uso anónimo. El correo guardado se envía con las solicitudes a MyMemory para contactar si hay problemas. Se guarda localmente, sin servidor del desarrollador. Cuotas según el proveedor, sin garantía individual.",
        ["memoryQuotaHint"] = "匿名 5,000 字符/天 · 选填邮箱可提至 50,000 · 在“管理服务”中设置|匿名 5,000 字元/天 · 選填信箱可提至 50,000 · 在「管理服務」設定|5,000 chars/day · Optional email: 50,000 · Set in Manage services|匿名 5,000 文字/日・メール提供で 50,000・サービス管理で設定|하루 5,000자 · 이메일 제공 시 50,000 · 서비스 관리에서 설정|5 000 car./jour · E-mail : 50 000 · Gérer les services|5.000 Zeichen/Tag · Mit E-Mail: 50.000 · Dienste verwalten|5.000 caracteres/día · Con correo: 50.000 · Gestionar servicios",
        ["memoryQuotaLink"] = "查看 MyMemory 官方额度说明|查看 MyMemory 官方額度說明|View official MyMemory limits|MyMemory の公式利用枠|MyMemory 공식 한도 보기|Voir les quotas officiels|Offizielle MyMemory-Kontingente|Ver límites oficiales de MyMemory",
        ["emailInvalid"] = "请输入有效邮箱，或留空继续匿名使用。|請輸入有效信箱，或留空以匿名使用。|Enter a valid email or leave blank for anonymous use.|有効なメールを入力するか、匿名利用には空欄にしてください。|올바른 이메일을 입력하거나 익명 사용을 위해 비워두세요.|Saisissez une adresse valide ou laissez vide.|Gültige E-Mail eingeben oder leer lassen.|Introduce un correo válido o déjalo vacío.",
        ["sourceGuess"] = "自动判断（可能不准）|自動判斷（可能不準）|Automatic guess (may be inaccurate)|自動推定（不正確な場合あり）|자동 추정 (부정확할 수 있음)|Estimation automatique|Automatische Schätzung|Estimación automática",
        ["resourceRegion"] = "资源区域|資源區域|Resource region|リソースのリージョン|리소스 지역|Région de la ressource|Ressourcenregion|Región del recurso",
        ["enableShortcut"] = "启用此快捷键|啟用此快速鍵|Enable this shortcut|このキーを有効化|이 단축키 사용|Activer ce raccourci|Dieses Kürzel aktivieren|Activar este atajo",
        ["readingRule"] = "划词：单词可用离线词典或 AI，句子与输入框翻译可选相同的翻译服务。|劃詞：單字可用離線詞典或 AI，句子與輸入框可選相同的服務。|Selection: words use the dictionary or AI; sentences and input fields share the same service options.|選択翻訳：単語は辞書か AI、文は入力欄と同じサービスを選べます。|단어는 사전 또는 AI, 문장과 입력란은 같은 서비스 옵션을 사용합니다.|Mots : dictionnaire ou IA. Phrases et saisie : mêmes choix de services.|Wörter: Wörterbuch oder KI. Sätze und Eingabefelder: dieselben Dienste.|Palabras: diccionario o IA. Frases y campos: las mismas opciones de servicio.",
        ["sentenceService"] = "句子翻译|句子翻譯|Sentences|文の翻訳|문장 번역|Phrases|Sätze|Frases",
        ["serviceType"] = "翻译服务类型|翻譯服務類型|Service type|サービスの種類|서비스 종류|Type de service|Diensttyp|Tipo de servicio",
        ["myMemorySetup"] = "MyMemory 无需填写 API 密钥。可直接在划词的句子翻译或输入框翻译中选择。|MyMemory 不必填寫 API 金鑰，可在句子或輸入框翻譯中選擇。|MyMemory needs no API key. Select it for sentence or input translation.|MyMemory は API キー不要です。文や入力欄の翻訳に選べます。|MyMemory는 API 키 없이 문장이나 입력란 번역에 선택할 수 있습니다.|MyMemory ne nécessite pas de clé API. Choisissez-le pour les phrases ou la saisie.|MyMemory benötigt keinen API-Schlüssel und steht für Sätze und Eingaben bereit.|MyMemory no necesita clave API. Elígelo para frases o entradas.",
        ["selectionFeature"] = "划词翻译|劃詞翻譯|Selection translation|選択して翻訳|선택 번역|Traduire la sélection|Auswahl übersetzen|Traducir selección",
        ["inputFeature"] = "输入框翻译|輸入框翻譯|Input translation|入力欄を翻訳|입력란 번역|Traduire la saisie|Eingabe übersetzen|Traducir entrada",
        ["softwareUpdate"] = "软件更新|軟體更新|Software updates|ソフトウェア更新|소프트웨어 업데이트|Mises à jour|Softwareupdates|Actualizaciones",
        ["donation"] = "关于轻风|關於輕風|About Breezy|Breezy について|Breezy 정보|À propos de Breezy|Über Breezy|Acerca de Breezy",
        ["generalSettings"] = "通用设置|通用設定|General|一般設定|일반 설정|Général|Allgemein|General",
        ["offlineDictionary"] = "离线词典|離線詞典|Offline dictionary|オフライン辞書|오프라인 사전|Dictionnaire hors ligne|Offline-Wörterbuch|Diccionario sin conexión",
        ["shortcutDisabled"] = "快捷键已禁用|快速鍵已停用|Shortcut disabled|ショートカット無効|단축키 사용 안 함|Raccourci désactivé|Tastenkürzel deaktiviert|Atajo desactivado",
        ["targetLanguage"] = "翻译成|翻譯成|Translate into|翻訳先|번역 언어|Traduire en|Übersetzen in|Traducir al",
        ["wordService"] = "单词翻译|單字翻譯|Single words|単語の翻訳|단어 번역|Mots seuls|Einzelne Wörter|Palabras individuales",
        ["sentenceAi"] = "句子翻译（AI）|句子翻譯（AI）|Sentences (AI)|文の翻訳（AI）|문장 번역 (AI)|Phrases (IA)|Sätze (KI)|Frases (IA)",
        ["defaultAi"] = "默认 AI 服务|預設 AI 服務|Default AI service|既定の AI サービス|기본 AI 서비스|Service IA par défaut|Standard-KI-Dienst|Servicio de IA predeterminado",
        ["manageServices"] = "管理服务|管理服務|Manage services|サービスを管理|서비스 관리|Gérer les services|Dienste verwalten|Gestionar servicios",
        ["configureAiFirst"] = "请先在 AI 服务页添加配置，并为此功能选择一个 AI 服务。|請先在 AI 服務頁新增設定，並為此功能選擇 AI 服務。|Add an AI service in settings, then select it for this feature.|設定で AI サービスを追加して、この機能に選択してください。|설정에서 AI 서비스를 추가한 뒤 이 기능에 선택하세요.|Ajoutez un service IA puis sélectionnez-le pour cette fonction.|KI-Dienst hinzufügen und für diese Funktion auswählen.|Añade un servicio de IA y selecciónalo para esta función.",
        ["selectionDemo"] = "划词翻译：看不懂，就选中|劃詞翻譯：看不懂，就選取|Reading: select an unfamiliar word|読む：分からない単語を選択|읽기: 모르는 단어 선택|Lecture : sélectionnez un mot|Lesen: unbekanntes Wort markieren|Lectura: selecciona una palabra",
        ["inputDemo"] = "整句翻译：直接在原输入框操作|整句翻譯：直接在原輸入框操作|Writing: translate in the original field|書く：元の入力欄で翻訳|쓰기: 원래 입력란에서 번역|Écriture : traduisez dans le champ|Schreiben: direkt im Eingabefeld übersetzen|Escritura: traduce en el mismo campo",
        ["selectionStep1"] = "1. 按住鼠标拖选不认识的词或句子|1. 按住滑鼠拖選不認識的詞或句子|1. Drag to select a word or sentence|1. 単語や文をドラッグして選択|1. 단어나 문장을 드래그하여 선택|1. Sélectionnez un mot ou une phrase|1. Wort oder Satz mit der Maus markieren|1. Arrastra para seleccionar una palabra o frase",
        ["selectionStep2"] = "2. 点击选区旁边出现的小气泡|2. 點選選取範圍旁的小氣泡|2. Click the bubble beside the selection|2. 選択部分の隣のボタンをクリック|2. 선택 영역 옆 말풍선 클릭|2. Cliquez sur la bulle à côté|2. Blase neben der Auswahl anklicken|2. Pulsa la burbuja junto a la selección",
        ["selectionStep3"] = "3. 就近弹出释义或译文，不用复制粘贴|3. 就近彈出釋義或譯文，免複製貼上|3. Read the nearby result; no copy and paste|3. 近くに意味や訳文が表示されます|3. 복사·붙여넣기 없이 바로 번역 확인|3. Lisez le résultat, sans copier-coller|3. Ergebnis direkt daneben lesen|3. Lee la traducción sin copiar y pegar",
        ["selectionStep4"] = "4. 点击喇叭听读音；点击卡片外即可关闭|4. 點喇叭聽讀音；點卡片外即可關閉|4. Click the speaker to listen; click outside to close|4. スピーカーで発音、外側をクリックで閉じる|4. 스피커로 발음 듣기 · 바깥을 눌러 닫기|4. Écoutez ; cliquez en dehors pour fermer|4. Aussprache anhören; außerhalb klicken zum Schließen|4. Escucha con el altavoz; pulsa fuera para cerrar",
        ["inputStep1"] = "1. 点击输入框，输入整句，不需要选中文字|1. 點選輸入框輸入整句，不必選取文字|1. Type a sentence; no selection needed|1. 入力欄に文を入力。選択は不要です|1. 문장 입력 · 텍스트 선택 불필요|1. Saisissez une phrase, sans la sélectionner|1. Satz eingeben, ohne Text zu markieren|1. Escribe una frase; no hace falta seleccionarla",
        ["inputStep2"] = "2. 点击这个输入框右上角的小气泡|2. 點選輸入框右上角的小氣泡|2. Click the bubble at the field’s top-right|2. 入力欄の右上のボタンをクリック|2. 입력란 오른쪽 위 말풍선 클릭|2. Cliquez sur la bulle en haut à droite|2. Blase oben rechts am Feld anklicken|2. Pulsa la burbuja arriba a la derecha del campo",
        ["inputStep3"] = "3. 助手翻译整个输入框的内容|3. 助手翻譯整個輸入框的內容|3. The assistant translates the whole field|3. 入力欄全体を翻訳します|3. 입력란 전체 내용을 번역|3. L’assistant traduit tout le champ|3. Der gesamte Feldinhalt wird übersetzt|3. El asistente traduce todo el campo",
        ["inputStep4"] = "4. 译文直接填回原输入框，可以继续编辑或发送|4. 譯文直接填回原輸入框，可繼續編輯或傳送|4. Translation replaces the text, ready to edit or send|4. 訳文が元の欄に入り、編集や送信ができます|4. 원래 입력란에 번역문 표시 · 편집 또는 전송|4. Le texte traduit remplace l’original, prêt à modifier|4. Übersetzung im selben Feld bearbeiten oder senden|4. La traducción queda en el campo para editar o enviar",
        ["pauseDemo"] = "暂停演示|暫停示範|Pause demonstration|デモを一時停止|데모 일시 정지|Mettre en pause|Demo pausieren|Pausar demostración",
        ["playDemo"] = "继续演示|繼續示範|Resume demonstration|デモを再開|데모 재개|Reprendre|Demo fortsetzen|Reanudar demostración",
        ["replayDemo"] = "重新播放|重新播放|Replay demonstration|最初から再生|데모 다시 재생|Rejouer|Demo neu starten|Repetir demostración",
        ["defaultService"] = "默认翻译服务|預設翻譯服務|Default translation service|既定の翻訳サービス|기본 번역 서비스|Service de traduction par défaut|Standard-Übersetzungsdienst|Servicio de traducción predeterminado",
        ["configureAi"] = "配置 AI 服务|設定 AI 服務|Configure AI|AI の設定|AI 설정|Configurer l’IA|KI konfigurieren|Configurar IA",
        ["offlineMode"] = "离线词典（英→中单词）|離線詞典（英→中單字）|Offline dictionary (EN→ZH words)|オフライン辞書（英→中の単語）|오프라인 사전 (영→중 단어)|Dictionnaire hors ligne (EN→ZH)|Offline-Wörterbuch (EN→ZH)|Diccionario sin conexión (EN→ZH)",
        ["followService"] = "跟随默认服务|跟隨預設服務|Use default service|既定のサービスを使用|기본 서비스 사용|Service par défaut|Standarddienst verwenden|Usar servicio predeterminado",
        ["online"] = "在线翻译|線上翻譯|Online translation|オンライン翻訳|온라인 번역|Traduction en ligne|Online-Übersetzung|Traducción en línea",
        ["offlineLimit"] = "离线词典仅支持英语单词译成简体中文。整句或其他语言请切换在线服务，或配置本机 AI（如 Ollama）。本次未联网。|離線詞典僅支援英文單字譯成簡體中文。整句或其他語言請選線上服務或本機 AI。本次未連線。|The offline dictionary supports English words into Simplified Chinese. For sentences or other languages, select an online service or local AI such as Ollama. No network request was sent.|オフライン辞書は英単語から簡体字中国語のみ対応。文章にはオンラインサービスかローカル AI を選んでください。通信していません。|오프라인 사전은 영어 단어를 간체 중국어로 번역합니다. 문장은 온라인 서비스나 로컬 AI를 선택하세요. 네트워크 요청은 없었습니다.|Le dictionnaire hors ligne traduit les mots anglais en chinois simplifié. Pour les phrases, utilisez un service en ligne ou une IA locale. Aucune requête envoyée.|Das Offline-Wörterbuch unterstützt englische Wörter auf Chinesisch. Für Sätze Online-Dienst oder lokale KI wählen. Keine Netzwerkanfrage gesendet.|El diccionario traduce palabras inglesas al chino simplificado. Para frases, usa un servicio en línea o IA local. No se envió ninguna solicitud.",
        ["aiServices"] = "通用 AI 服务|通用 AI 服務|AI services|AI サービス|AI 서비스|Services IA|KI-Dienste|Servicios de IA",
        ["aiSaved"] = "已保存的配置|已儲存的設定|Saved configurations|保存した設定|저장된 설정|Configurations enregistrées|Gespeicherte Konfigurationen|Configuraciones guardadas",
        ["aiPreset"] = "服务预设|服務預設|Provider preset|サービスのプリセット|서비스 프리셋|Fournisseur prédéfini|Anbietervorlage|Proveedor predefinido",
        ["aiName"] = "配置名称|設定名稱|Configuration name|設定名|설정 이름|Nom de configuration|Konfigurationsname|Nombre de configuración",
        ["aiAddress"] = "接口地址|介面位址|Base URL|API の URL|API 주소|URL de base|Basis-URL|URL base",
        ["aiKey"] = "API 密钥|API 金鑰|API key|API キー|API 키|Clé API|API-Schlüssel|Clave API",
        ["aiModel"] = "模型|模型|Model|モデル|모델|Modèle|Modell|Modelo",
        ["aiDefault"] = "设为默认 AI 配置|設為預設 AI 設定|Use as default AI configuration|既定の AI 設定にする|기본 AI 설정으로 사용|Configuration IA par défaut|Als KI-Standard verwenden|Usar como IA predeterminada",
        ["aiLoad"] = "获取模型列表|取得模型清單|Load models|モデル一覧を取得|모델 목록 가져오기|Charger les modèles|Modelle laden|Cargar modelos",
        ["aiTest"] = "测试翻译|測試翻譯|Test translation|翻訳をテスト|번역 테스트|Tester la traduction|Übersetzung testen|Probar traducción",
        ["aiAdd"] = "添加|新增|Add|追加|추가|Ajouter|Hinzufügen|Añadir",
        ["aiDelete"] = "删除|刪除|Delete|削除|삭제|Supprimer|Löschen|Eliminar",
        ["aiApplyModel"] = "应用模型名称|套用模型名稱|Apply model|モデル名を適用|모델 이름 적용|Appliquer le modèle|Modell übernehmen|Aplicar modelo",
        ["retranslate"] = "重译|重譯|Retry|再翻訳|재번역|Retraduire|Erneut|Retraducir",
        ["aiHint"] = "选择服务 → 填写密钥 → 获取或填写模型 → 保存。\n测试会发送 Hello，可能产生少量费用。密钥在本机加密保存。\n支持 Chat 兼容接口与 Claude Messages；本机服务可不填密钥。|選擇服務 → 填寫金鑰 → 取得或填寫模型 → 儲存。\n測試傳送 Hello，可能產生費用。金鑰在本機加密。\n支援 Chat 相容介面與 Claude Messages；本機可不填金鑰。|Choose provider → enter key → load or enter model → save.\nTest sends Hello and may incur a small charge. Keys are encrypted locally.\nSupports Chat Completions and Claude Messages; local keys are optional.|サービス → キー → モデル → 保存。テストは Hello を送信し、料金が発生する場合があります。キーは端末内で暗号化。Chat 互換と Claude Messages に対応。|서비스 → 키 → 모델 → 저장. 테스트는 Hello를 전송하며 요금이 발생할 수 있습니다. 키는 로컬에서 암호화됩니다. Chat 호환 및 Claude Messages 지원.|Fournisseur → clé → modèle → enregistrer. Le test envoie Hello et peut être facturé. Clés chiffrées localement. Chat Completions et Claude Messages pris en charge.|Anbieter → Schlüssel → Modell → Speichern. Der Test sendet Hello und kann Kosten verursachen. Schlüssel lokal verschlüsselt. Chat Completions und Claude Messages unterstützt.|Proveedor → clave → modelo → guardar. La prueba envía Hello y puede tener coste. Claves cifradas localmente. Admite Chat Completions y Claude Messages.",
        ["alreadyRunning"] = "程序已经运行，请查看系统托盘。|程式已在執行，請查看系統匣。|The app is already running. Check the system tray.|アプリは起動済みです。通知領域を確認してください。|앱이 이미 실행 중입니다. 시스템 트레이를 확인하세요.|L’application est déjà ouverte. Consultez la zone de notification.|Die App läuft bereits. Bitte im Infobereich nachsehen.|La aplicación ya está abierta. Revisa la bandeja del sistema.",
        ["pronounce"] = "朗读|朗讀|Pronounce|読み上げ|발음 듣기|Prononcer|Vorlesen|Pronunciar",
        ["autoReadHelp"] = "开启后自动朗读；关闭后点击喇叭播放。|開啟後自動朗讀；關閉後點喇叭播放。|Read automatically when enabled; use the speaker for manual playback.|オンで自動読み上げ、オフでスピーカーをクリック。|켜면 자동으로 읽고, 끄면 스피커를 눌러 재생합니다.|Activez pour lire automatiquement ; sinon cliquez sur le haut-parleur.|Automatisch vorlesen oder manuell auf das Lautsprechersymbol klicken.|Activa la lectura automática o pulsa el altavoz manualmente.",
        ["wordMissing"] = "本地词典未收录这个单词，请检查拼写。|本機詞典未收錄此單字，請檢查拼字。|This word is not in the offline dictionary. Check its spelling.|辞書にない単語です。つづりを確認してください。|오프라인 사전에 없는 단어입니다. 철자를 확인하세요.|Mot absent du dictionnaire. Vérifiez l’orthographe.|Wort nicht im Offline-Wörterbuch. Schreibweise prüfen.|La palabra no está en el diccionario. Revisa la ortografía.",
        ["missingVoice"] = "请安装对应的 Windows 语音：|請安裝對應的 Windows 語音：|Install a Windows speech voice for:|Windows 音声をインストールしてください：|Windows 음성 팩을 설치하세요:|Installez une voix Windows pour :|Windows-Sprachstimme installieren für:|Instala una voz de Windows para:",
        ["noSelection"] = "请先选中文字，再点击附近的气泡或使用快捷键。|請先選取文字，再點氣泡或使用快速鍵。|Select text first, then click the nearby bubble or use the shortcut.|文字を選択し、近くのボタンかショートカットを使ってください。|텍스트를 선택한 뒤 말풍선이나 단축키를 사용하세요.|Sélectionnez du texte, puis utilisez la bulle ou le raccourci.|Zuerst Text markieren, dann Blase oder Tastenkürzel verwenden.|Selecciona texto y usa la burbuja o el atajo.",
        ["noPhonetic"] = "暂无音标|暫無音標|Phonetic spelling unavailable|発音記号なし|발음 기호 없음|Phonétique indisponible|Lautschrift nicht verfügbar|Fonética no disponible",
        ["requiredKey"] = "请填写 API 密钥，或选择当前试用服务。|請填寫 API 金鑰，或選擇目前試用服務。|Enter an API key or choose the prototype service.|API キーを入力するか試用サービスを選択してください。|API 키를 입력하거나 시제품 서비스를 선택하세요.|Entrez une clé API ou choisissez le service de prototype.|API-Schlüssel eingeben oder Prototyp-Dienst wählen.|Introduce una clave API o elige el servicio de prueba.",
        ["actionFailed"] = "无法完成操作，请检查输入、服务配置或软件兼容性。|無法完成操作，請檢查輸入、服務設定或軟體相容性。|Could not complete the action. Check the text, service settings, or app compatibility.|操作できません。入力、サービス設定、アプリの対応状況を確認してください。|작업을 완료할 수 없습니다. 입력, 서비스 설정, 앱 호환성을 확인하세요.|Impossible de terminer. Vérifiez le texte, le service ou la compatibilité.|Aktion fehlgeschlagen. Text, Dienst oder App-Kompatibilität prüfen.|No se pudo completar. Revisa el texto, el servicio o la compatibilidad.",
        ["app"] = "轻风翻译|輕風翻譯|Breezy Translate|そよ風翻訳|산들바람 번역|Breezy Translate|Breezy Translate|Breezy Translate",
        ["appShort"] = "轻风|輕風|Breezy|Breezy|Breezy|Breezy|Breezy|Breezy",
        ["aboutIntro"] = "轻风是一款 Windows 桌面翻译工具，帮助你划词查词、翻译句子和输入框内容。\n如果它对你有所帮助，欢迎自愿捐赠，支持后续开发与维护。|輕風是一款 Windows 桌面翻譯工具，協助你查詢選取的單字、翻譯句子與輸入框內容。\n如果它對你有所幫助，歡迎自願捐贈，支持後續開發與維護。|Breezy is a Windows desktop tool for selected text and input translation.\nIf it helps you, you can support its development with a voluntary donation.|Breezy は、選択したテキストや入力欄の内容を翻訳する Windows デスクトップツールです。\n開発・保守を支援する任意の寄付を受け付けています。|Breezy는 선택한 텍스트와 입력란의 내용을 번역하는 Windows 데스크톱 도구입니다.\n자발적인 기부로 개발과 유지보수를 지원할 수 있습니다.|Breezy est un outil Windows qui traduit le texte sélectionné et le contenu des champs de saisie.\nVous pouvez soutenir son développement par un don volontaire.|Breezy ist ein Windows-Tool zum Übersetzen von markiertem Text und Eingabefeldern.\nMit einer freiwilligen Spende können Sie die Entwicklung unterstützen.|Breezy es una herramienta de Windows para traducir texto seleccionado y campos de entrada.\nPuedes apoyar su desarrollo con una donación voluntaria.",
        ["bubbleAccessible"] = "轻风：选中文字后点击查词，在输入框内点击翻译，拖动可固定位置|輕風：選取文字後點擊查詞，在輸入框內點擊翻譯，拖曳可固定位置|Breezy: click to translate selected text or an input field; drag to fix its position|Breezy：選択したテキストや入力欄をクリックして翻訳。ドラッグして位置を固定|Breezy: 클릭하여 선택한 텍스트나 입력란 번역, 드래그하여 위치 고정|Breezy : cliquez pour traduire la sélection ou un champ ; faites glisser pour fixer la position|Breezy: Auswahl oder Eingabefeld per Klick übersetzen; zum Fixieren ziehen|Breezy: haz clic para traducir la selección o un campo; arrastra para fijar la posición",
        ["settings"] = "设置|設定|Settings|設定|설정|Paramètres|Einstellungen|Ajustes",
        ["languages"] = "语言与预览|語言與預覽|Languages & preview|言語とプレビュー|언어 및 미리보기|Langues et aperçu|Sprachen & Vorschau|Idiomas y vista previa",
        ["shortcuts"] = "快捷键|快速鍵|Keyboard shortcuts|ショートカット|단축키|Raccourcis|Tastenkürzel|Atajos",
        ["service"] = "翻译服务|翻譯服務|Translation service|翻訳サービス|번역 서비스|Service de traduction|Übersetzungsdienst|Servicio de traducción",
        ["interface"] = "界面语言|介面語言|Interface language|表示言語|화면 언어|Langue de l’interface|Anzeigesprache|Idioma de la interfaz",
        ["read"] = "选中文字后，翻译成|選取文字後，翻譯成|Translate selected text into|選択したテキストの翻訳先|선택한 텍스트의 번역 언어|Traduire la sélection en|Markierten Text übersetzen in|Traducir la selección al",
        ["write"] = "输入框里的内容，翻译成|輸入框裡的內容，翻譯成|Translate input-box text into|入力欄の翻訳先|입력란 내용의 번역 언어|Traduire le texte saisi en|Eingabetext übersetzen in|Traducir lo escrito al",
        ["readAction"] = "阅读：选中文字 → 点附近气泡|閱讀：選取文字 → 點附近氣泡|Reading: select text → click the nearby bubble|読む：文字を選択 → 近くのボタン|읽기: 텍스트 선택 → 옆의 말풍선 클릭|Lecture : sélection → bulle à proximité|Lesen: Text markieren → Blase anklicken|Leer: selecciona texto → pulsa la burbuja",
        ["writeAction"] = "写作：点输入框 → 点框角气泡|寫作：點輸入框 → 點框角氣泡|Writing: focus a field → click its corner bubble|書く：入力欄 → 端のボタン|쓰기: 입력란 클릭 → 모서리 말풍선 클릭|Écriture : champ de saisie → bulle du coin|Schreiben: Eingabefeld → Blase in der Ecke|Escribir: campo de texto → burbuja de la esquina",
        ["demo"] = "操作示意 · 不发送翻译请求|操作示意 · 不傳送翻譯請求|Illustration only · no translation request|操作例・翻訳通信は行いません|사용 예시 · 번역 요청 없음|Démonstration · aucune requête|Vorschau · keine Übersetzungsanfrage|Ejemplo · sin solicitud de traducción",
        ["save"] = "保存并应用|儲存並套用|Save & apply|保存して適用|저장 및 적용|Enregistrer|Speichern|Guardar y aplicar",
        ["cancel"] = "取消|取消|Cancel|キャンセル|취소|Annuler|Abbrechen|Cancelar",
        ["enableKeys"] = "启用全局快捷键|啟用全域快速鍵|Enable global shortcuts|グローバルショートカットを有効化|전역 단축키 사용|Activer les raccourcis globaux|Globale Tastenkürzel aktivieren|Activar atajos globales",
        ["keyHint"] = "点击下面的框，然后直接按下想用的组合键。|點選下方欄位，再按下想用的組合鍵。|Click a field below, then press your preferred key combination.|欄をクリックし、使いたいキーの組み合わせを押します。|아래 칸을 클릭한 뒤 사용할 키 조합을 누르세요.|Cliquez dans un champ, puis appuyez sur les touches souhaitées.|Feld anklicken und die gewünschte Tastenkombination drücken.|Pulsa un campo y después la combinación de teclas deseada.",
        ["readKey"] = "翻译选中的文字|翻譯選取的文字|Translate selected text|選択したテキストを翻訳|선택한 텍스트 번역|Traduire la sélection|Markierten Text übersetzen|Traducir la selección",
        ["writeKey"] = "翻译当前输入框|翻譯目前輸入框|Translate the focused input box|入力中の欄を翻訳|현재 입력란 번역|Traduire le champ actif|Aktuelles Eingabefeld übersetzen|Traducir el campo activo",
        ["keyRule"] = "至少包含 Ctrl 或 Alt；不使用 F12；两项不能相同。|至少包含 Ctrl 或 Alt；不使用 F12；兩項不能相同。|Include Ctrl or Alt; no F12 or duplicate combinations.|Ctrl または Alt を含め、F12 と重複は避けてください。|Ctrl 또는 Alt를 포함하세요. F12와 중복 조합은 사용할 수 없습니다.|Incluez Ctrl ou Alt, sans F12 ni doublons.|Ctrl oder Alt verwenden; kein F12 und keine doppelten Belegungen.|Incluye Ctrl o Alt; evita F12 y combinaciones duplicadas.",
        ["serviceHint"] = "MyMemory 仅作试用，原文语言为初步判断，可在弹窗更正。Azure 支持正式自动识别，需要自己的配置。|MyMemory 僅供試用，原文語言可在彈窗更正。Azure 自動辨識需設定。|MyMemory is a prototype fallback; correct its source guess in the popup. Azure provides automatic detection and requires configuration.|MyMemory の推定言語は修正できます。Azure の自動検出には設定が必要です。|MyMemory의 추정 언어는 수정할 수 있습니다. Azure 자동 감지는 설정이 필요합니다.|MyMemory estime la langue. Azure nécessite votre configuration.|MyMemory schätzt die Sprache. Azure benötigt eine Konfiguration.|MyMemory estima el idioma. Azure necesita configuración.",
        ["apiKey"] = "Azure API 密钥|Azure API 金鑰|Azure API key|Azure API キー|Azure API 키|Clé API Azure|Azure-API-Schlüssel|Clave API de Azure",
        ["region"] = "资源区域，例如 eastasia|資源區域，例如 eastasia|Resource region, e.g. eastasia|リソースのリージョン|리소스 지역|Région de la ressource|Ressourcenregion|Región del recurso",
        ["keySaved"] = "已有加密配置；留空保留|已有加密設定；留空保留|Encrypted key saved; leave blank to keep|保存済み・空欄で維持|암호화된 키 저장됨 · 비워두면 유지|Clé enregistrée ; laisser vide pour conserver|Gespeichert; leer lassen zum Beibehalten|Clave guardada; deja en blanco para conservar",
        ["commercialHint"] = "使用你自己的翻译服务账户，费用与额度由服务商管理。密钥在本机加密保存，不经过开发者服务器。|使用自己的服務帳戶，費用與額度由服務商管理。金鑰在本機加密儲存。|Use your own service account. Your provider manages billing and quotas. Keys are encrypted locally; no developer server is involved.|ご自身のサービスアカウントを使用します。料金は提供元が管理し、キーは端末内で暗号化されます。|본인 서비스 계정을 사용합니다. 요금은 제공업체가 관리하며 키는 로컬에 암호화됩니다.|Utilisez votre compte fournisseur. Facturation gérée par le fournisseur, clés chiffrées localement.|Eigenes Dienstkonto verwenden. Abrechnung beim Anbieter, Schlüssel lokal verschlüsselt.|Usa tu cuenta del proveedor. El proveedor gestiona los cargos; las claves se cifran localmente.",
        ["auto"] = "自动识别|自動辨識|Detect language|自動検出|자동 감지|Détection automatique|Automatisch erkennen|Detectar idioma",
        ["source"] = "原文语言|原文語言|Source language|原文の言語|원문 언어|Langue source|Ausgangssprache|Idioma de origen",
        ["target"] = "翻译成|翻譯成|Translate into|翻訳先|번역 언어|Traduire en|Übersetzen in|Traducir al",
        ["translate"] = "翻译|翻譯|Translate|翻訳|번역|Traduire|Übersetzen|Traducir",
        ["copy"] = "复制|複製|Copy|コピー|복사|Copier|Kopieren|Copiar",
        ["use"] = "使用译文|使用譯文|Use translation|訳文を使用|번역문 사용|Utiliser|Übernehmen|Usar traducción",
        ["autoRead"] = "自动朗读|自動朗讀|Auto-read|自動読み上げ|자동 읽기|Lecture auto|Automatisch lesen|Lectura automática",
        ["offline"] = "离线词典|離線詞典|Offline dictionary|オフライン辞書|오프라인 사전|Dictionnaire hors ligne|Offline-Wörterbuch|Diccionario sin conexión",
        ["translation"] = "译文|譯文|Translation|訳文|번역문|Traduction|Übersetzung|Traducción",
        ["manual"] = "手动输入|手動輸入|Enter text|テキスト入力|직접 입력|Saisir du texte|Text eingeben|Introducir texto",
        ["exit"] = "退出|結束|Exit|終了|종료|Quitter|Beenden|Salir",
        ["help"] = "使用说明|使用說明|Help|ヘルプ|도움말|Aide|Hilfe|Ayuda",
        ["follow"] = "自动靠近选中文字和输入框|自動靠近選取文字和輸入框|Follow selected text and input fields|選択と入力欄に追従|선택한 텍스트와 입력란 따라가기|Suivre la sélection et les champs|Auswahl und Eingabefeldern folgen|Seguir selección y campos",
        ["bottom"] = "输入框按钮放在右下角|輸入框按鈕放在右下角|Place input button at bottom-right|入力ボタンを右下に配置|입력 버튼을 오른쪽 아래에 배치|Bouton en bas à droite|Schaltfläche unten rechts|Botón abajo a la derecha",
        ["conflict"] = "快捷键已被占用，请换一个组合；原设置仍保留。|快速鍵已被占用，請更換組合。|Shortcut is in use. Choose another; previous settings remain active.|このキーは使用中です。別の組み合わせを選んでください。|사용 중인 단축키입니다. 다른 조합을 선택하세요.|Raccourci déjà utilisé. Choisissez une autre combinaison.|Tastenkürzel bereits belegt. Bitte ein anderes wählen.|El atajo ya está en uso. Elige otra combinación.",
        ["saveFailed"] = "无法保存设置，请检查目录权限。|無法儲存設定，請檢查權限。|Could not save settings. Check folder permissions.|設定を保存できません。|설정을 저장할 수 없습니다.|Impossible d’enregistrer les paramètres.|Einstellungen konnten nicht gespeichert werden.|No se pudieron guardar los ajustes."
    };
    public static string T(string key, string? language = null)
    {
        language ??= UserPreferences.Current.InterfaceLanguage;
        int index = Array.IndexOf(InterfaceLanguages, language);
        return Values.TryGetValue(key, out var line) ? line.Split('|')[index < 0 ? 2 : index] : key;
    }
    static readonly Dictionary<string, string> Original = new()
    {
        ["自动朗读"] = "autoRead", ["离线词典"] = "offline", ["翻译"] = "translate", ["复制"] = "copy", ["使用译文"] = "use", ["译文"] = "translation"
    };
    public static void Apply(Control root)
    {
        if (root is not RichTextBox && Original.TryGetValue(root.Text, out var key)) root.Text = T(key);
        if (root is AutoReadSwitch) root.Width = Math.Clamp(TextRenderer.MeasureText(root.Text, root.Font).Width + 42, 116, 180);
        if (root is SpeakerButton) root.AccessibleName = T("pronounce");
        foreach (Control child in root.Controls) Apply(child);
    }
    public static string ErrorMessage(Exception error)
    {
        if (error.Message.StartsWith(T("missingVoice")) || error.Message == T("offlineLimit") || error.Message == T("configureAiFirst") || error.Message == T("emailInvalid")) return error.Message;
        if (error.Message.Contains("未收录")) return T("wordMissing");
        if (UserPreferences.Current.InterfaceLanguage.StartsWith("zh")) return error.Message;
        var code = System.Text.RegularExpressions.Regex.Match(error.Message, "HTTP [0-9]{3}");
        return T("actionFailed") + (code.Success ? " (" + code.Value + ")" : "");
    }
}

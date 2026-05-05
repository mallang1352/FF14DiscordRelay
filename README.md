# FF14 Discord Relay

FF14 Discord Relay is a small Windows utility that reads selected FFXIV chat
lines from local ACT log files and forwards them to a Discord webhook.

It does not hook the Discord client, modify FFXIV, read game memory, or alter
network packets. The relay works only with local ACT log files and a webhook URL
provided by the user.

## 한국어 사용 설명

### 준비물

- Windows x64 PC
- ACT(Advanced Combat Tracker)와 FFXIV 플러그인
- Discord Webhook URL

Self-contained ZIP 배포본은 .NET 런타임을 포함합니다. ZIP 압축을 푼 뒤
폴더 안의 `FF14DiscordRelay.exe`를 실행하면 됩니다. exe 파일만 따로
꺼내서 실행하면 필요한 DLL을 찾지 못할 수 있습니다.

### 최초 실행

1. 프로그램을 실행하면 설정 창이 열립니다.
2. ACT가 실행 중인지 확인합니다.
3. Webhook URL을 입력합니다.
4. ACT 로그 폴더와 내 닉네임은 자동 감지를 우선 사용합니다.
5. 가져올 채팅 구분을 선택합니다.
6. 테스트 버튼으로 Webhook 전송을 확인합니다.
7. 저장 버튼을 누르고 설정 암호를 만듭니다.
8. 시작 버튼을 누르면 중계가 시작됩니다.

설정은 아래 위치에 암호화되어 저장됩니다.

```text
%APPDATA%\FF14DiscordRelay\config.enc
```

### 동작 방식

- 이 프로그램은 Discord 클라이언트를 후킹하지 않습니다.
- FFXIV 메모리, 패킷, 게임 클라이언트를 조작하지 않습니다.
- ACT가 만든 로컬 로그 파일에서 선택한 채팅만 읽습니다.
- 사용자가 입력한 Discord Webhook으로 메시지를 보냅니다.
- Discord 멘션은 기본적으로 비활성화됩니다.

### 닉네임 감지

닉네임은 ACT 로그의 현재 플레이어 정보에서 자동 감지합니다. 캐릭터를
변경하면 중계 중에도 새 닉네임을 감지해 설정창과 저장 설정을 갱신합니다.

닉네임이 자동 감지되지 않으면 시작과 Webhook 테스트가 막힙니다. ACT와
캐릭터 로그인 상태를 먼저 확인해 주세요.

### 트레이 동작

창의 X 버튼을 누르면 프로그램은 종료되지 않고 트레이로 숨겨집니다. 트레이
아이콘을 더블 클릭하거나 메뉴에서 설정 열기를 선택하면 다시 설정창이
나타납니다.

종료하려면 트레이 메뉴의 종료를 사용합니다.

### 제한 해제 모드

초기 설정은 안전한 기본값으로 잠겨 있습니다.

- 메시지 형식 고정
- ACT 프로세스명 고정
- 내 닉네임 자동 감지 사용
- 가져올 채팅 구분 1개만 선택
- 필수 옵션 고정

설정창 상단 로고 이미지를 연속 5번 클릭하면 제한 해제 모드가 활성화되어
일부 항목을 직접 수정할 수 있습니다.

### 설정 초기화

비밀번호 입력 창에서 기존 설정 삭제 버튼을 사용할 수 있습니다. 이 작업은
저장된 Webhook URL, ACT 설정, 채팅 구분, 옵션을 삭제하며 복구할 수
없습니다.

### 배포 파일 주의

공유할 때는 `config.enc`, ACT 로그, Webhook URL, 개인 채팅 스크린샷을
포함하지 마세요. Release ZIP과 SHA256 해시를 함께 제공하는 방식을
권장합니다.

## Features

- Reads selected ACT FFXIV chat channels.
- Can forward only the current player's own messages.
- Detects the current character name from ACT log data.
- Updates the nickname while running when the logged-in character changes.
- Sends messages through a Discord webhook.
- Suppresses Discord mentions by default.
- Queues messages when Discord rate limits are hit, then sends the backlog as
  grouped messages after the retry delay.
- Stores settings encrypted under the current Windows user's AppData folder.
- Supports single-instance behavior and restores the settings window when a
  second copy is launched.

## Privacy

Webhook URLs and settings are stored locally in encrypted form:

```text
%APPDATA%\FF14DiscordRelay\config.enc
```

Do not publish or share your own `config.enc`, ACT logs, webhook URL, character
names, or screenshots that include private chat.

## Requirements

- Windows x64
- Advanced Combat Tracker with FFXIV log output
- Discord webhook URL

Self-contained ZIP builds include the .NET runtime. Framework-dependent builds
require the matching Microsoft .NET Desktop Runtime.

## ACT Input

Auto detection checks the running ACT process and common ACT configuration files
under:

```text
%APPDATA%\Advanced Combat Tracker\Config
```

The relay watches ACT log lines such as:

```text
00|timestamp|code|name|line|...
[time] ChatLog 00:code:name:line
```

Nickname detection prefers ACT network line `02`:

```text
02|timestamp|actorId|characterName|...
```

If that line is not available, recent chat senders from the selected channels
are used as fallback candidates.

## Discord Output

The restricted default message template is:

```text
[{name} say] : {message}
```

Unlocked mode allows custom templates using:

```text
{channel}
{code}
{name}
{message}
```

## Building

Build:

```powershell
dotnet build -c Release
```

Self-contained folder publish:

```powershell
dotnet publish -c Release -r win-x64 -p:SelfContained=true -p:PublishSingleFile=false -p:PublishReadyToRun=false -o publish\win-x64-share-friendly --force
```

Single-file publish:

```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:PublishReadyToRun=false -o publish\win-x64-portable --force
```

## Distribution Notes

Unsigned Windows executables can trigger antivirus, SmartScreen, or Discord file
warnings. Prefer publishing releases through a stable project page such as
GitHub Releases and include SHA256 hashes for downloadable files. For broader
distribution, use a trusted code-signing certificate.

# FF14 Discord Relay

FF14 Discord Relay is a small Windows utility that reads selected FFXIV chat
lines from local ACT log files and forwards them to a Discord webhook.

It does not hook the Discord client, modify FFXIV, read game memory, or alter
network packets. The relay works only with local ACT log files and a webhook URL
provided by the user.

## 한국어 사용 설명

### 다운로드

GitHub 저장소의 `Releases` 페이지에서 최신 버전 ZIP 파일을 다운로드하세요.

현재 배포 버전:

```text
v1.0.9
```

권장 다운로드 파일:

```text
FF14DiscordRelay-v1.0.9-win-x64.zip
```

SHA256:

```text
611217BC57E8B5DE30856B85F4A944F06FFA0FE8F5986F7EE2299D125C0A5152
```

ZIP 파일을 받은 뒤 압축을 풀고, 폴더 안의 `FF14DiscordRelay.exe`를
실행하세요. exe 파일만 따로 꺼내서 실행하지 마세요.

### 업데이트 안내

프로그램 실행 시 GitHub Releases의 최신 버전을 확인합니다. 현재 설치된
버전보다 새 버전이 있으면 다운로드 페이지를 열지 물어봅니다.

업데이트 파일을 자동으로 다운로드하거나 자동 실행하지는 않습니다. 안내가
나오면 GitHub Release 페이지에서 ZIP 파일을 직접 다운로드한 뒤 기존
폴더를 새 버전 폴더로 교체해 주세요.

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

### Discord Webhook URL 만드는 방법

Webhook URL은 Discord 채널로 메시지를 보낼 수 있는 전용 주소입니다. 이
주소를 가진 사람은 해당 채널에 메시지를 보낼 수 있으므로 외부에 공개하지
마세요.

1. Discord에서 메시지를 받을 서버를 엽니다.
2. 메시지를 받을 채널을 고릅니다.
3. 채널 이름 옆의 톱니바퀴 또는 `채널 편집`을 엽니다.
4. 왼쪽 메뉴에서 `연동` 또는 `Integrations`를 선택합니다.
5. `웹후크` 또는 `Webhooks`를 선택합니다.
6. `새 웹후크` 또는 `New Webhook`을 누릅니다.
7. 웹후크 이름을 정합니다. 예: `FF14 Discord Relay`
8. 메시지를 보낼 채널이 맞는지 확인합니다.
9. `웹후크 URL 복사` 또는 `Copy Webhook URL`을 누릅니다.
10. 복사한 URL을 FF14 Discord Relay의 `Webhook` 입력칸에 붙여넣습니다.
11. `테스트` 버튼을 눌러 Discord 채널에 테스트 메시지가 오는지 확인합니다.

Webhook 메뉴가 보이지 않는다면 다음을 확인하세요.

- 해당 서버에서 `웹후크 관리` 권한이 있는지 확인합니다.
- 개인 DM에는 Webhook을 만들 수 없습니다. 서버 채널에서 만들어야 합니다.
- 서버 관리자가 Webhook 생성을 막아둔 경우 사용할 수 없습니다.

Webhook URL은 비밀번호처럼 취급하세요. 실수로 공개했다면 Discord의
Webhook 설정에서 기존 Webhook을 삭제하고 새로 만들어야 합니다.

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

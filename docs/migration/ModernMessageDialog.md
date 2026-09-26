# ModernMessageDialog 교체 가이드

- **대체 대상**: `System.Windows.Forms.MessageBox`
- **네임스페이스**: `Modern.Lab.WinForms.Controls.Dialogs`
- **본문 컨트롤**: `Modern.Lab.Controls.Wpf.Display.ModernMessageDialogControl`
  (아이콘 원 + 제목 + 본문. 창·버튼은 호스트 폼이 담당)

## 왜 필요했나 (2026-08-04)

서버 실패 사유는 **길다**. 이전에는 화면 안 배지(`ModernStatusBadge`)에 넣었는데
배지는 한 줄이라 잘리고, 잘린 뒤에는 **툴팁으로만** 읽을 수 있었다 — 사실상 읽을 수
없었다. 토스트도 답이 아니다: 자동으로 사라지므로 **반드시 읽어야 하는 사유**를
담을 수 없다.

| 무엇을 보여주나 | 어디에 |
|---|---|
| 흘려도 되는 짧은 성공/진행 안내 | 토스트 (`ShowToast`) |
| 화면 상태를 나타내는 짧은 한 줄 | 배지 (`ModernStatusBadge`) |
| **반드시 읽어야 하는 실패 사유·확인 질문** | **이 다이얼로그** |

## 호환 제공 멤버 (MessageBox 대응)

| MessageBox | ModernMessageDialog |
|---|---|
| `MessageBox.Show(owner, msg)` | `ModernMessageDialog.ShowInformation(owner, msg)` |
| `MessageBox.Show(owner, msg, caption, OK, Information)` | `ShowInformation(owner, caption, msg)` |
| `… MessageBoxIcon.Warning` | `ShowWarning(owner, caption, msg)` |
| `… MessageBoxIcon.Error` | `ShowError(owner, caption, msg)` |
| `… MessageBoxButtons.YesNo, Warning) == DialogResult.Yes` | `Confirm(owner, caption, msg)` → `bool` |
| (없음) | `Emphasis(text)` — 본문 안 낱말을 **액센트색 SemiBold**로 강조하는 표기(`**text**`)를 만든다 (2026-08-29 추가). "OffLine → **OnLineLocal**"처럼 무엇으로 바꾸는지를 눈에 띄게. 복사(Ctrl+C·복사 버튼)와 창 높이 계산은 표기를 걷어낸 순수 본문을 쓴다. 본문은 읽기 전용 `RichTextBox`라 선택·복사는 그대로다 |
| (없음) | `ShowError(owner, caption, msg, details)` — 본문은 **현업이 읽고 행동할 수 있는 안내**로 쓰고, 서버 응답 원문·전문 이름·예외 문구는 `details`로 넘긴다. 상세는 접혀서 열린다 |
| 그 밖의 조합 | `Show(owner, kind, caption, msg, buttons)` · 상세까지는 `Show(owner, kind, caption, msg, buttons, details)` |
| Ctrl+C 전체 복사 | 동일하게 동작 (제목+본문 평문; 본문 박스 우측 위 복사 버튼도 같은 일) |

`buttons`는 `OK` / `OKCancel` / `YesNo`를 지원한다. 그 밖의 값(`YesNoCancel`,
`RetryCancel`, `AbortRetryIgnore`)은 **확인 하나**로 처리한다 — 3버튼 다이얼로그는
"무엇이 기본인지" 판단을 사용자에게 넘기므로 쓰지 않는 것이 이 프로젝트의 방침이다.

## 종류 (ModernMessageKind)

| 값 | 아이콘 | 색 | 용도 |
|---|---|---|---|
| `Information` | i | 파랑 | 알림·안내 |
| `Success` | 체크 | 초록 | 처리 완료를 **반드시** 알릴 때 (가벼우면 토스트) |
| `Warning` | 느낌표 | 주황 | 주의·되돌릴 수 있는 문제 |
| `Error` | X | 빨강 | 처리 실패·서버 거절 사유 |
| `Question` | ? | 파랑 | 예/아니오 승인 |

## 폼 베이스를 쓰는 화면은 이렇게

`ModernFormBase`가 이미 감싸 두었으므로 화면은 네임스페이스를 몰라도 된다:

```csharp
if (!result.Success)
{
    this.ShowErrorMessage("Receive failed", result.Message);   // 긴 사유도 그대로
    return;
}

if (!this.Confirm("선택한 3건을 삭제할까요?", "Delete"))   // 모던 확인창
{
    return;
}
```

`ModernFormBase.Confirm`은 **이 다이얼로그로 바뀌었다**(예전에는 OS `MessageBox`).
회사 표준 알림창으로 교체할 때는 `ShowMessage`/`Confirm` **두 메서드만** 재정의하면
전 화면에 반영된다.

## 특징

- 본문은 테두리가 있는 **복사 박스** 안에서 자동 줄바꿈되고, 길면 세로 스크롤이 나타난다.
  약 네 줄까지는 스크롤 없이 보이고, 본문 컨트롤은 남은 높이를 채우므로 창의 높이 상한에 도달해도
  내용이 잘리지 않는다. 복사 아이콘은 스크롤바 왼쪽의 박스 안에 고정된다.
- 본문은 **드래그해 복사**할 수 있다(읽기 전용 `TextBox`) — 사유를 이슈에 붙일 때.
- **원클릭 복사 + Ctrl+C** (2026-08-23 현장 보고 반영): 본문 박스 우측 위 복사 버튼 또는
  Ctrl+C가 **제목+본문을 평문으로** 클립보드에 담는다 — 네이티브 `MessageBox`의
  Ctrl+C 전체 복사 호환을 복원한 것이다. 본문에 드래그 선택이 있는 상태의 Ctrl+C는
  선택 부분만 복사한다(WPF가 먼저 처리 — 선택이 우선). 복사에 성공하면 버튼
  아이콘이 잠깐 체크로 바뀐다. 클립보드가 잠겨 있으면(원격 데스크톱·클립보드 관리
  도구) 재시도 후 조용히 실패한다 — 앱은 죽지 않는다.
- **기술 상세는 접어 둔다** (2026-09-16): `details`를 넘기면 버튼 줄 **왼쪽 끝**에
  `Details` 버튼이 생기고, 누르면 창이 **버튼 줄 아래로** 자라면서 읽기 전용 상세 박스가
  나온다. 상세 박스는 **본문 박스와 같은 생김새**(옅은 배경·연한 테두리·둥근 모서리, 본문 박스와
  같은 왼쪽 선)이고 박스 안 오른쪽 위에 복사 아이콘이 있다(2026-09-27, 전에는 단선 입력칸 + `Copy` 버튼).
  다시 누르면 접히고 창도 원래 크기로 돌아간다. 상세의 복사 아이콘은 제목·본문·상세를
  **한 번에** 담으므로(상세만 보내면 어느 화면 무슨 상황인지 알 수 없다) 현업에게는
  "Details 누르고 복사 아이콘 눌러서 보내 주세요" 한 마디면 된다. 복사에 성공하면 아이콘이 잠깐
  체크로 바뀐다. 상세 박스는 처음 펼칠 때 만든다. `details`가 비어 있으면 버튼 자체가 생기지 않는다 — 화면 코드가 조건을
  따질 필요 없이 있으면 넘기면 된다.
  **현업이 읽을 문구와 개발자가 받을 원문을 가르는 것이 이 멤버의 목적이다.** 누락 컬럼 이름이나
  계약 용어를 본문에 올리면 현업은 읽고도 할 수 있는 일이 없다.
- **Enter = 기본 버튼, Esc = 취소 버튼** — `ModernButton`이 `IButtonControl`이라
  폼의 `AcceptButton`/`CancelButton`으로 처리된다(별도 키 처리 코드 없음).
- **제목만 종류 색**으로 물든다 (오류=빨강, 경고=주황, 성공=초록, 정보/확인=기본).
  본문은 항상 기본 글자색이다 — 긴 사유 전체가 빨간 글씨면 읽기 어렵고 과하다.
- **본문 글자는 14px**(제목 16px). WPF 글리프는 같은 크기의 GDI 글자보다 옅게 그려져 12px 본문이
  흐려 보인다는 현장 보고(2026-09-03)가 있었고, 화면 픽셀을 세 보니 진한 획이 GDI의 3분의 1이었다.
  14px에서 GDI+ 라벨과 같은 무게가 된다(실측 — `Modern.Lab.Samples.exe --diag-textcapture`).
- **OS 제목줄을 쓰지 않는다** (`FormBorderStyle.None`, 2026-08-04):
  회색 캡션이 구식으로 보이고 제목이 캡션과 본문에 두 번 나왔다. 대신
  **얇은 헤더(34px)에 닫기 ✕**를 두고 헤더를 드래그해 창을 옮긴다. 복사 버튼은 본문 박스 안에 둔다.
  1px 테두리로 형태를 잡고, Windows 11에서는 모서리가 둥글다(구형 OS는 각짐).
- **닫기 ✕는 Esc와 같은 결과**를 낸다 (알림=확인, 확인창=No) — 닫기가 별도 정책이
  되면 같은 창에서 결과가 갈린다.
- 제목을 주지 않으면 종류별 기본 제목(`Error`/`Warning`/…)이 창 이름
  (`Form.Text` — Alt+Tab·접근성 도구용)에 들어간다.

## 미지원과 대체 방법

| 기존 | 대체 |
|---|---|
| `MessageBoxIcon.Hand`/`Stop`/`Exclamation` 등 별칭 | `ModernMessageKind`의 5종으로 정리 |
| `MessageBoxDefaultButton` | 종류별로 고정 (알림=확인, 확인=Yes) |
| `MessageBoxOptions`(RTL 등) | 없음 |
| 3버튼 조합 | 화면에서 두 단계로 나눈다 (예: 확인 → 별도 옵션 선택) |

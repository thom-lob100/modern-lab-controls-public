# 임시 전달 — Equipment/Lots (2026-09-15, 갱신 10)

> **이 브랜치에서 받으세요 — `transfer-2026-09-14`.**
> 저장소를 그냥 열면 `main` 이 보이는데 거기에는 이 파일들이 없습니다(2026-09-14 에 실제로 헛걸음이
> 있었습니다). 주소: `https://github.com/thom-lob100/modern-lab-controls-public/tree/transfer-2026-09-14`

## 지금 받아 갈 파일 — **여섯**

갱신 9 를 적은 뒤 세 회차가 더 올라갔습니다. 아래가 지금 이 브랜치에 있는 전부입니다. 갱신 9 까지만
받아 두셨다면 **`Hosting/` 두 개가 새로 늘었고**, `EquipmentLotForm.cs` 와 `TableMerge.cs` 와
`EquipmentLotPresenter.cs` 는 그때 받으신 것보다 더 바뀌었습니다.

| 파일 | 바뀐 것 |
|---|---|
| `Management/EquipmentLotForm.cs` | Lot 클릭 시 카드를 비우지 않고 로딩 표시로 덮는다 · Remarks 두 줄 · 결정 값을 카드 제목에 적고 대상 줄이 반짝인다 |
| `Management/EquipmentLotForm.Designer.cs` | 의뢰서 카드 로딩 표시(`requestBusy`) 추가 · Remarks 영역 스크롤 |
| `Management/Services/TableMerge.cs` | 키 없는 표도 제자리에서 갱신한다(시편 표 깜빡임) · 자리 순서 갱신을 키를 선언하지 않은 표로만 좁혔다 |
| `Management/Services/EquipmentLotPresenter.cs` | 통신 모드를 Lot 의 장비에서 읽는다 · Start/End 의 장비 일치 조건 제거 · 결정 지속재가 작업의 타깃을 따른다 |
| `Hosting/ModernFormBase.cs` | 마지막 알림이 화면 하단에 한 줄로 남는다 |
| `Hosting/ModernFormBase.Messaging.cs` | 사용자가 누른 것을 거절할 때 토스트가 아니라 모달로 알린다 |

`Hosting/` 두 파일은 **베이스만 바뀌고 화면 소스는 한 줄도 바뀌지 않습니다.**

### 이미 받아 가신 것 (마지막 세 회차에서 한 줄도 바뀌지 않았습니다)

| 파일 | 무엇 |
|---|---|
| `Management/Services/JobDecision.cs` | 결정이 장비 목록을 들고 다닌다(`EquipmentList`) |
| `Management/Contracts/EquipmentLotContracts.cs` | 게이트가 보는 상태 컬럼 목록 |
| `Management/Contracts/ServerFields.cs` | 장비 행의 `GOAL_PORT_NM` |

받은 파일이 맞는지 확인: `EquipmentLotForm.cs` 에 **`sameRequest` 가 2 번**, `RequestRemarkHeight` 가 있고
값이 **64** 면 갱신 9 이후 것입니다.

`ServerFields.cs` 는 이번 전달에 넣지 않았습니다. 홈에서는 상위 Common 으로 옮겼지만 회사 적용은
따로 정하기로 했으므로, 전달본에서는 그 이동에 딸린 `using` 한 줄을 빼 두었습니다.

## 0. 거절은 토스트가 아니라 모달입니다 — `Hosting/ModernFormBase.Messaging.cs`

사용자가 누른 것을 거절해 놓고 2.5 초짜리 토스트로 알리고 있었습니다. 눈을 돌리면 못 보고, 그러면 왜
아무 일도 일어나지 않았는지 알 길이 없습니다. 거절을 내는 두 자리(쓰기 시작 거절 · 처리 중 창 닫기
거절)의 종류를 `Info` 에서 `Warning` 으로 바꿨습니다. `ShowToast` 진입점이 `Warning` 을 모달로 돌리므로
**호출부는 그대로**입니다.

## 0. 마지막 알림이 하단에 한 줄로 남습니다 — `Hosting/ModernFormBase.cs`

토스트는 2.5 초면 사라져서 눈을 돌리면 무슨 일이 있었는지 알 수 없습니다. 베이스가 하단에 한 줄짜리
라벨을 만들어 붙이고 마지막 알림을 시각과 함께 남깁니다 — 실패와 거절은 빨강, 그 밖은 평범한 글자색
입니다. 토스트로 가든 모달로 가든 그 줄에는 남습니다. 라벨은 첫 알림 때 만들어 `Dock=Bottom` 으로
붙으므로 **화면 소스는 손대지 않습니다.**

## 0. 결정 지속재는 작업의 타깃을 따릅니다 — `Services/EquipmentLotPresenter.cs`

작업이 걸린 Lot 이 결정이면 지속재도 그 작업의 타깃(`GOAL_CARRIER_ID`)입니다. 지금까지는 목록에서 고른
값이나 1순위가 그대로 남아, 카드 제목과 판정이 그 작업과 무관한 지속재를 가리켰습니다. 소스
(`CARRIER_ID`)와 타깃(`GOAL_CARRIER_ID`)을 가르는 규칙이 요약 줄에만 있고 결정에는 없던 것이 원인입니다.

## 0. 결정 값을 카드 제목에 적고 대상 줄이 반짝입니다 — `EquipmentLotForm.cs` · `Services/TableMerge.cs`

목록에서 고른 것과 결정된 것을 눈으로 가릅니다. 같은 회차에서 `TableMerge` 는 자리 순서 갱신을 **키를
선언하지 않은 표로만** 좁혔습니다.

## 0. Lot 을 클릭할 때도 의뢰서 카드가 깜빡이지 않습니다

자동 갱신 쪽은 앞 갱신으로 멎었는데, **사람이 다른 Lot 을 고를 때는 그대로였습니다.** 카드를 먼저
비우고 서버를 불렀기 때문에 왕복 동안 빈 카드가 보였습니다.

- 이제 **이전 내용을 둔 채 로딩 표시를 덮고**, 응답이 정착하면 한 번에 바꿉니다.
- 덮는 순간 최신 성공 스냅숏을 버리므로 그 사이 **의뢰서 팝업은 열리지 않습니다.** 낡은 값을 지금
  값으로 오인시키지 않기 위한 것입니다.
- 조회가 **실패하면 그때 비웁니다.**
- 고른 Lot 의 `REQ_SERIAL_NO` 가 지금 보고 있는 것과 같으면 **전문조차 나가지 않고** 카드도 건드리지
  않습니다.

디자이너에 추가된 것은 로딩 표시 하나입니다.

```csharp
this.requestBusy = new Modern.Lab.WinForms.Controls.Display.ModernBusyOverlay();
this.requestCard.Controls.Add(this.requestBusy);
this.requestBusy.Message = "Loading request...";
this.requestBusy.Visible = false;
```

## 0. Remarks 는 두 줄, 넘치면 그 안에서 스크롤합니다

시편 목록이 너무 짧다는 지적에 따라 헤더가 차지하던 자리를 표에 돌려줬습니다.

- Remarks 영역을 네 줄(104)에서 **두 줄(64)** 로 줄였습니다.
- 두 줄보다 긴 글은 **잘리지 않고 그 영역 안에서 세로로 스크롤**합니다. 짧은 글에서는 스크롤바가
  보이지 않습니다.
- 헤더 상한이 184 에서 **144** 로 내려가고 시편 표가 그만큼 커집니다 — 홈 기준 316px, 같은 창에서
  표에 두 줄이 더 보입니다.

디자이너에서 바뀐 것은 두 줄입니다.

```csharp
this.panelRequestRemark.AutoScroll = true;          // 넘치면 스크롤
this.lblRequestRemark.AutoSize = true;              // 내용만큼 자란다
this.lblRequestRemark.Dock = DockStyle.Top;         // Fill 이면 갇혀서 넘침을 모른다
```

## 0. 시편 표가 자동 갱신마다 깜빡이던 진짜 원인 — `Services/TableMerge.cs`

의뢰서 카드를 비우지 않게 고친 뒤에도 **시편 표의 행이 사라졌다 다시 나타나는** 잔깜빡임이 남았습니다.
원인은 한 단계 아래였습니다. `TableMerge.Apply` 가 **행 키가 있는 표만** 값을 비교해 고치고, 키가 없는
표(시편 표가 그렇습니다)는 `Rows.Clear()` 뒤 전부 다시 넣고 있었습니다. 표 객체는 그대로여서 "제자리
갱신" 으로 보였지만, 행이 전멸했다 부활하므로 그리드는 목록이 통째로 바뀐 것으로 받아 다시 그립니다.

이제 키 없는 표도 **자리 순서로 짝지어** 값이 달라진 칸만 쓰고, 남거나 모자라는 만큼만 지우고 더합니다.
키 있는 표가 쓰던 `UpdateRow` 를 그대로 쓰므로 새 규칙이 아닙니다.

홈에서 갱신 한 번을 통과시키며 잰 값입니다.

```
고치기 전   마스터행 유지 True · 시편행 유지 False · 컬럼 유지 False
고친 뒤     마스터행 유지 True · 시편행 유지 True  · 컬럼 유지 True
```

파일 하나를 통째로 받으시면 되고, 회사 사본의 `UpdateRow` 인자가 셋이면 마지막 인자만 빼고 부르면
됩니다.

## 0. Job 버튼은 결정 장비가 아니라 **고른 Lot**을 봅니다

작업이 걸린 Lot(`JOB_STATE`가 `JobPrep`·`JobStart`)을 고르면 결정 패널은 **그 Lot 행 하나가 들고 있는
네 값**을 그대로 씁니다.

| 무엇 | 컬럼 |
|---|---|
| 장비 | `EQP_ID` |
| In 포트 | `PORT_NM` |
| Out 포트 | `GOAL_PORT_NM` |
| 지속재 | `CARRIER_ID` |

- Job Start·End 에 걸려 있던 **"결정 장비와 Lot 의 장비가 같아야 한다"는 조건을 없앴습니다.** 지금까지는
  다른 장비를 고른 채로는 진행 중인 작업을 시작·종료할 수 없었습니다.
- `JobPrep` 이면 Job Start, `JobStart` 면 Job End 만 열립니다. 작업이 없는 Lot은 예전처럼 준비 모드입니다.
- **통신 모드(`COMM_STAT_TYP`)는 여전히 장비에서만 읽습니다.** 다만 Lot 의 `EQP_ID` 로 장비 목록에서 찾은
  행에서 읽고, 그 행을 못 찾으면 세 버튼을 모두 닫습니다(장비 목록이 도착하면 다시 판정합니다).

| 통신 모드 | Job Prep | Job Start | Job End |
|---|---|---|---|
| `OnLineLocal` | 열림 | 열림 | 열림 |
| `OnLineRemote` | 열림 | 닫힘 | 닫힘 |
| `OffLine` | 닫힘 | 닫힘 | 닫힘 |

- 실행 모드에서는 **장비·포트·지속재를 더블클릭해도 결정 패널이 바뀌지 않습니다.** 목록의 선택과 표시만
  따라갑니다.
- 화면은 준비 당시 값을 기억하지 않습니다. 값이 바뀌면 다음 Lot 갱신에 실려 옵니다.
- 전문 형상 게이트도 업무 게이트와 **같은 장비 행**을 봅니다. 둘이 갈라지면 결격 장비에서 Job Start 가
  열리거나 무관한 장비 때문에 닫힐 수 있어 함께 맞췄습니다.

## 0. 의뢰서 카드가 자동 갱신에 깜빡이지 않습니다

자동 갱신이 돌 때마다 의뢰서 마스터·시편 표를 **비웠다 다시 채우느라** 카드가 한 번 비었습니다.

- 같은 `REQ_SERIAL_NO`를 다시 받는 조용한 갱신은 이제 **제자리에서 맞춥니다.** 표 객체를 갈아끼우지 않고
  행도 누적되지 않습니다.
- 시편 표는 행 키가 없으므로 **자리 순서**로 맞춥니다.
- 조회가 실패하면 예전처럼 두 표를 비웁니다. 낡은 값을 성공처럼 남기지 않습니다.

같은 자리에서 헤더도 손봤습니다.

- 마스터 필드리스트는 **마지막 줄에 한 칸만 남는 개수**일 때 3열에서 4열로 늘립니다.
- Remarks 영역을 **네 줄**로 넓혔습니다.
- 디테일(시편) 표는 창이 작아도 **머리와 몇 줄이 남는 최소 높이**를 보장받고, 헤더가 그보다 커지면 헤더
  쪽이 스크롤합니다. 작은 창에서 분할 위치 예외가 나던 자리입니다.

### 바뀐 파일 넷

| 파일 | 무엇 |
|---|---|
| `Management/EquipmentLotForm.cs` | 실행 모드 더블클릭 가드 · 형상 게이트 장비 일치 · 의뢰서 제자리 갱신 · 헤더 열 수와 높이 |
| `Management/Services/EquipmentLotPresenter.cs` | 이번에 처음 넣습니다 — `JobEquipment` 한 자리에서 통신 모드를 읽고, Start·End 의 장비 일치 조건을 뺐습니다 |
| `Management/Services/JobDecision.cs` | 결정이 장비 목록을 들고 다니게 `EquipmentList` 추가 |
| `Management/Contracts/EquipmentLotContracts.cs` | 게이트가 요구하는 상태 컬럼 목록(통신 모드·포트 상태·Lot 상태 등) |

`Management/Contracts/EquipmentLotContracts.cs` 는 **회사 네임스페이스(`Modern.Lab.Samples.Management.*`)를
그대로 두었습니다.** 나머지 셋은 이전 전달분과 같은 형태입니다.

**캡처**(홈 시드, 09-14 검사 실행에서 뜬 그대로):
`shots/equipment-lots-2026-09-14.png` 전체 화면 · `shots/equipment-lots-2026-09-14-small.png` 작은 창에서
시편 표가 머리와 몇 줄을 남기는 모습 · `shots/equipment-lots-2026-09-14-request-dialog.png` 의뢰서 팝업.

**홈 검사**: `--uitest-equipment-lot` `PASS 209 / FAIL 10`. 남은 10건은 이번 변경 전부터 있던 것으로
같은 목록이 그대로이고(Refresh 버튼 폭, 홈 시드에서만 나는 더블클릭·Lock 항목), 이번에 더한 검사는
모두 통과합니다.

## 0. 장비 행의 포트로 결정 패널을 즉시 채운다

장비를 더블클릭하면 결정 패널이 **포트 조회 왕복을 기다린 뒤 지속재 조회까지 한 번 더** 기다렸습니다
(`ResolveDecisionPorts`가 포트 행이 와야 In/Out 을 정할 수 있었고, 포트가 오면 `SyncDecisionDurablesIfReady`가
지속재를 또 불렀습니다). 그래서 30초까지 걸릴 수 있었습니다.

이제 **장비 행이 들고 온 포트 이름**을 씁니다.

| 무엇 | 컬럼 |
|---|---|
| In 포트 | `PORT_NM` |
| Out 포트 | `GOAL_PORT_NM` |

- 더블클릭 즉시 결정 패널의 `In Port → Out Port`가 채워집니다. **서버 왕복 0회.**
- 포트 목록이 도착하면 **그 이름과 같은 포트 행**을 잡습니다(없으면 예전처럼 우선순위 첫 포트).
- 포트를 더블클릭하면 그 방향만 교체됩니다(Input→In, Output→Out, InputOutput→둘 다). 기존 동작 그대로입니다.
- 포트 도착이 지속재 조회를 끌지 않습니다. 그룹이 같으면 지속재 타입이 같으므로 다시 받을 이유가 없습니다.

### 바뀐 파일 셋

| 파일 | 무엇 | 분량 |
|---|---|---|
| `Management/Contracts/ServerFields.cs` | `Equipment.GoalPortNm = "GOAL_PORT_NM"` | 한 줄 |
| `Management/Services/JobDecision.cs` | `InPortNm`·`OutPortNm`이 포트 행이 없으면 장비 행 값을 쓴다 | 열 줄 남짓 |
| `Management/EquipmentLotForm.cs` | `ResolveDecisionPorts`가 추천 이름과 같은 행을 잡는다 · `BindPorts`에서 지속재 동기화 호출 제거 | 열다섯 줄 남짓 |

디자이너는 안 건드렸습니다(`476166e` 그대로).

**홈 확인**: 홈 API 장비 조회에도 두 컬럼을 실어서 화면에 `Port`·`Goal Port`로 보입니다.
캡처 `shots/equipment-lots-ports.png`.

## 0. 실행 패널이 Lot 리스트 아래까지 온다 — 중첩을 뒤집었습니다

앞선 갱신에서는 안쪽 스플리터를 내려서 **실행 패널이 의뢰서 아래에만** 걸쳤습니다. 스플리터 중첩
순서를 바꿔 고쳤습니다.

- 바깥 스플리터(`splitRight`) = **[장비+Lot | 포트+의뢰서] 대 [지속재+결정]** — 이게 맨 위부터
  실행 줄까지 내려옵니다.
- 안쪽 스플리터(`splitMain`) = 장비 대 포트·의뢰서 — 바깥 스플리터의 왼쪽 칸 안으로 들어갔습니다.
- 실행 카드는 바깥 왼쪽 칸 바닥, Job 카드는 바깥 오른쪽 칸 바닥.

결과: 실행 패널이 **Lot 리스트 아래부터 의뢰서 아래까지** 한 줄로 이어지고, Job 카드는 결정 열
아래에 붙습니다. 픽셀로 재 보면 위 경계와 아래 경계가 같은 자리입니다.

실행 카드의 Equipment·Port 드롭다운도 잘리지 않게 설계 위치를 64px 왼쪽으로 옮겼습니다.

**캡처**: `shots/equipment-lots-wide.png` (2560×1200 전체) · `shots/equipment-lots-bottom.png` (하단 줄)

## 0-1. 최초 로드에서 Lot·지속재가 다시 뜹니다 — `EquipmentLotForm.cs`

앞선 갱신에서 더블클릭 연쇄를 끊으면서 **최초 조회의 Lot·지속재 조회까지 같이 떨어져 나갔습니다.**
그 조회를 결정 경로가 아니라 **조회 경로**(`BindEquipments` 끝)로 옮겼습니다.

```csharp
            this.LoadPorts(this.SelectedEquipmentId(), this.silentRefresh);
            this.ApplyEquipmentDecision();
            this.LoadDecisionLots(this.SelectedGroupId(), this.decision.EqpId);   // ← 추가
            this.SyncDecisionDurables(false);                                    // ← 추가
```

- 화면을 열 때·조회 버튼·자동 갱신에서 네 목록이 모두 뜹니다.
- 더블클릭은 여전히 자기 것만 결정합니다(장비→포트, Lot→Lot, 지속재→지속재).

## 0. 스플리터가 맨 아래 실행 줄까지 내려옵니다 — `EquipmentLotForm.Designer.cs`

의뢰서와 결정 패널을 가르던 그 스플리터 **하나**가 이제 아래 실행 줄까지 지납니다. 새 스플리터를
만들지 않았고 폼 코드도 필요 없습니다.

- `bottomPanel`(폼 바닥에 따로 붙어 있던 줄)과 그 위 `gapBottom`을 없앴습니다.
- 실행 카드는 `splitRight.Panel1` 바닥에, Job 카드는 `splitRight.Panel2` 바닥에 붙습니다
  (둘 다 `Dock = Bottom`, 각 패널 안에 8px 간격 패널).
- 그래서 결정 패널과 Job 카드는 **언제나 같은 폭**이고, 스플리터를 끌면 둘이 같이 움직입니다.

**한 가지 눈에 보이는 변화** — 실행 카드가 가운데 열 폭으로 줄어들고, 왼쪽 장비·Lot 열이 그만큼
(약 64px) 아래까지 내려옵니다. 실행 줄이 스플리터 안으로 들어갔으니 구조상 따라오는 결과입니다.

파일 둘입니다. `Modern.Lab.Commons.dll`은 바뀌지 않습니다.

## 1. 더블클릭이 서로 끌고 가지 않는다 — `EquipmentLotForm.cs`

장비를 더블클릭하면 Lot·의뢰서·지속재 목록이 통째로 다시 조회되고 있었습니다. 결정 장비가 바뀌면
`ClearDecisionDependents()`가 Lot·지속재 표를 비우고 `LoadDecisionLots()`가 그 장비 기준으로 Lot 을
다시 받고 지속재가 따라가는 구조였습니다.

이제 이렇게 동작합니다.

| 무엇을 더블클릭 | 무엇이 따라오나 |
|---|---|
| 장비 | **포트만** — 결정 장비·포트가 잡히고 결정 패널이 갱신됩니다 |
| 포트 | 그 포트(필요하면 장비도) — Lot·지속재는 그대로 |
| Lot | **Lot만** |
| 지속재 | **지속재만** |

의뢰서 목록은 전과 같이 **Lot 선택**을 따라갑니다(결정이 아니라 선택입니다).

지운 호출은 넷입니다.

- `ApplyEquipmentDecision` — `ClearDecisionDependents()` · `LoadDecisionLots(...)` · `SyncDecisionDurables(false)`
- 포트 더블클릭(장비가 바뀌는 가지) — `ClearDecisionDependents()` · `LoadDecisionLots(...)` · `SyncDecisionDurables(false)`
- 포트 더블클릭(같은 장비 가지) — `SyncDecisionDurables(false)`
- `OnLotRowDoubleClick` — `SyncDecisionDurables(false)`

Lot·지속재 목록은 조회 버튼과 자동 갱신 주기에서만 다시 받습니다.

## 2. 의뢰서 목록 깜빡임 — 같은 파일에 포함

`BindRequestFields`가 조회할 때마다 필드 카드를 통째로 다시 만들던 것을 고쳤습니다. 컬럼 구성이 그대로면
다시 만들지 않고 값만 넣고, 높이도 실제로 달라질 때만 건드립니다.

## 3. 결정 패널 — `EquipmentLotForm.Designer.cs` (앞선 전달분 그대로)

- KPI 카드 넷의 `Flat` 제거 — 테두리·배경 복구(카드 79→96 · 둘째 줄 91→108 · 그리드 166→176 ·
  결정 카드 300→262).
- 지속재 목록 최소 높이 138 → 80 — 창을 낮추면 목록이 줄고 결정 패널이 따라 올라옵니다.

## 검증 상태

홈에서 **솔루션 빌드만** 확인했습니다. 화면 자체 검사는 돌리지 않았습니다.

## 파일

| 파일 | 무엇 |
|---|---|
| `Modern.Lab.Samples/Management/EquipmentLotForm.cs` | 1·2번 |
| `Modern.Lab.Samples/Management/EquipmentLotForm.Designer.cs` | 3번 |

# 임시 전달 — Equipment/Lots (2026-09-13, 갱신 5)

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

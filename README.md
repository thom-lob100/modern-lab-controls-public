# 임시 전달 — Equipment/Lots 화면 디자이너 (2026-09-13)

**이 저장소는 라이브러리 공개용이 아니라 단발 전달 통로입니다.** 급한 화면 변경을 회사에서 바로
보고 적용하기 위한 것이고, 정식 배포는 릴리즈 zip입니다.

이번에는 **`EquipmentLotForm.Designer.cs` 한 파일만** 2026-09-10 전달분에서 현재 버전으로
갱신했습니다. 나머지 세 파일(`EquipmentLotForm.cs` · `EquipmentLotContracts.cs` ·
`RequestInfoDialogForm.cs`)은 그대로 두었습니다. **`Modern.Lab.Commons.dll`은 바뀌지 않습니다.**

## 이번에 바뀐 것 — 결정 패널

### 1. KPI 카드의 테두리·배경

v0.58.0 에서 카드 넷에 `Flat = true`가 들어가 **테두리와 배경이 통째로 사라져** 있었습니다.
그 속성은 공용 카드 패널 위에 얹을 때 크롬을 없애라고 만든 것입니다. 네 줄을 지워 되돌리고,
크롬이 돌아온 만큼 자리를 함께 줬습니다.

| 무엇 | 전 | 후 |
|---|---|---|
| KPI 카드 높이(`kpiEquipment`·`kpiPorts`·`kpiLot`·`kpiDurable`) | 79 | **96** |
| 둘째 줄 Y(`kpiLot`·`kpiDurable`) | 91 | **108** |
| KPI 그리드(`decisionGrid`) 높이 | 166 | **176** |
| 결정 카드(`decisionCard`) 높이 | 300 | **262** |

### 2. 창을 낮추면 지속재 목록이 줄고 결정 패널이 따라 올라온다

지속재 목록의 **최소 높이 138px**이 바닥이라, 세로가 줄어도 목록이 더는 줄지 못해 결정 패널이
아래로 밀려 잘렸습니다. 바닥을 낮췄습니다. 높이 배분 자체는 폼의 `FitDecisionPanel`이 그대로
맡아 결정 패널 높이를 먼저 지키고 남는 높이를 목록에 줍니다.

| 무엇 | 전 | 후 |
|---|---|---|
| 목록 최소 높이(`splitDurableDecision.Panel1MinSize`) | 138 | **80** |
| 결정 패널 최소 높이(`splitDurableDecision.Panel2MinSize`) | 300 | **262** |

## 함께 들어오는 v0.58.0 손질

2026-09-10 전달분 이후 릴리즈에서 고친 배치도 이 파일에 함께 들어 있습니다.

- 왼쪽 목록 열과 포트 열의 최소 높이 214 → 120 — 낮은 작업 영역에서 위 표만 줄었다가 복원됩니다.
- `splitRight.FixedPanel = Panel2` · `Panel1MinSize` 360 → 230 — 오른쪽 실행 열 폭을 보존하고
  가로 변화는 왼쪽이 흡수합니다. 바깥 `splitMain.KeepRatio`는 그대로입니다.
- Job 버튼 셋이 `jobActions` 패널에 묶여 Job 카드 안으로 들어갔습니다.

**그래서 이 디자이너는 v0.58.0 기준의 `EquipmentLotForm.cs`와 짝입니다.** 회사 폼 소스가
2026-09-10 전달분 그대로라면 이 파일만 바꾸지 마시고 알려 주세요 — 짝이 되는 `.cs`도 함께
올리겠습니다.

## 확인해 주실 것

- 카드 테두리·배경이 돌아왔는지, 카드 안 내용이 잘리지 않는지.
- 창을 낮췄을 때 목록이 먼저 줄고 결정 패널이 따라 올라오는지.

## 파일

| 파일 | 무엇 |
|---|---|
| `Modern.Lab.Samples/Management/EquipmentLotForm.Designer.cs` | 이번 갱신분 — 위 내용 전부 |
| `Modern.Lab.Samples/Management/EquipmentLotForm.cs` | 2026-09-10 전달분 그대로 |
| `Modern.Lab.Samples/Management/Contracts/EquipmentLotContracts.cs` | 2026-09-10 전달분 그대로 |
| `Modern.Lab.Samples/RequestInfoDialogForm.cs` | 2026-09-10 전달분 그대로 |

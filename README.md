# 임시 전달 — 핫픽스 (2026-09-14 · 2026-09-15)

> 이 저장소에는 **회사가 지금 받아 갈 핫픽스만** 둡니다. 지난 회차 파일과 컨트롤 라이브러리는
> 두지 않습니다. 파일 경로는 회사 폴더 구조 그대로입니다.

## A. 신규 행 표시를 행 배경 틴트로 (2026-09-15)

Durable·Lot 목록의 신규 표시가 `New` 배지 **컬럼**에서 **행 배경 틴트**로 바뀝니다. 컬럼이 목록 폭을
먹던 것이 출발점이고, 무엇이 신규인지 가르는 규칙(같은 조회 조건의 직전 성공 결과와의 차집합)은
그대로입니다. Equipment Port 는 장비 트리에 행 색 경로가 없어 **배지 컬럼을 유지**합니다.

틴트 색은 처음에 액센트(파랑)를 섞었는데 선택 배경(`#B6D9F2`)과 같은 계열이라 **사용자가 신규 행을
선택된 행으로 읽었습니다.** 지금은 `Brush.Success` 를 섞은 `#DAF0E2` 로 계열을 갈랐습니다.

| 파일 | 무엇 |
|---|---|
| `Management/Services/NewItemTint.cs` | **새 파일** — 행 틴트 색을 만든다 · 틴트 컬럼 이름 |
| `Management/Services/NewItemTracker.cs` | 신규 판정 — 표시 문구를 상수로 뺐다(동작 동일, **안 받으셔도 됩니다**) |
| `Management/LotManagementForm.cs` · `Management/LotManagementForm.Contracts.cs` | Lot 목록의 신규 표시를 행 틴트로 |
| `Management/DurableManagementForm.cs` | Durable 목록의 신규 표시를 행 틴트로 |
| `Management/Services/LotManagementPresenter.cs` · `Management/Services/DurableManagementPresenter.cs` | 틴트 컬럼을 화면이 더한 컬럼 목록에 넣는다 |

`NewItemTint.cs` 는 **새 파일이라 프로젝트 등록**이 필요합니다. `RowColorMember` 는 이미 나가 있는
컨트롤 속성이라 **DLL 은 교체하지 않으셔도 됩니다.**

### 파일을 덮지 않고 손으로 옮기실 때 — 고칠 자리 여섯

폼이 회사 쪽에서 많이 갈라져 있으면 아래만 옮기셔도 됩니다.

**Lot 폼 `BindLots`** — 판정 결과를 틴트로 감싸고 행 색 컬럼을 알려준다

```csharp
// 전
this.lotData = this.newItems.Accept(canonical, ServerFields.Lot.LotId, this.newItemCriteria, judged);

// 후
this.lotData = NewItemTint.Apply(
        this.newItems.Accept(canonical, ServerFields.Lot.LotId, this.newItemCriteria, judged), judged);
this.gridLots.RowColorMember = NewItemTint.Member(this.lotData);
```

**Lot 폼 같은 메서드** — 옛 배지 컬럼 분기를 감추기 한 줄로

```csharp
// 전
if (this.newItems.HasNewItems) { columns.First(NewItemTracker.ColumnName).Badge(NewItemTracker.ColumnName); }
else { columns.Hide(NewItemTracker.ColumnName); }

// 후
columns.Hide(NewItemTracker.ColumnName, NewItemTint.ColumnName);
```

**Durable 폼 `BindDurables`** — 같은 모양

```csharp
// 전
this.durableData = this.newItems.Accept(this.durableData, ServerFields.Durable.DurableId, this.newItemCriteria, judged);

// 후
this.durableData = NewItemTint.Apply(
        this.newItems.Accept(this.durableData, ServerFields.Durable.DurableId, this.newItemCriteria, judged), judged);
this.gridDurables.RowColorMember = NewItemTint.Member(this.durableData);
```

**Durable 폼 같은 메서드** — 배지 목록에서 빼고 감추기로

```csharp
// 전
GridColumns.Of(this.durableData)
        .Badge(ServerFields.Durable.DurableStatCd, ServerFields.Durable.WfLoadStatCd.Column, NewItemTracker.ColumnName);
if (this.newItems.HasNewItems) { columns.First(NewItemTracker.ColumnName); }
else { columns.Hide(NewItemTracker.ColumnName); }

// 후
GridColumns.Of(this.durableData)
        .Badge(ServerFields.Durable.DurableStatCd, ServerFields.Durable.WfLoadStatCd.Column)
        .Hide(NewItemTracker.ColumnName, NewItemTint.ColumnName);
```

**프리젠터 둘** — `ScreenColumns` 에 틴트 컬럼 추가 (화면이 더한 컬럼이라 빠뜨리면 계약 검증이 걸립니다)

```csharp
// 전
new List<string> { TableJudgment.IssueColumn, NewItemTracker.ColumnName }.AsReadOnly();

// 후
new List<string> { TableJudgment.IssueColumn, NewItemTracker.ColumnName, NewItemTint.ColumnName }.AsReadOnly();
```

**`LotManagementForm.Contracts.cs`** — 컬럼 제외 조건에 한 줄 (Durable 은 해당 자리가 없습니다)

```csharp
&& !string.Equals(column.ColumnName, NewItemTracker.ColumnName, StringComparison.OrdinalIgnoreCase)
&& !string.Equals(column.ColumnName, NewItemTint.ColumnName, StringComparison.OrdinalIgnoreCase))
```

**확인** — 조회하면 신규 행이 `New` 컬럼 없이 연한 녹색 배경으로 뜨고, 선택하면 선택색으로 덮입니다.
두 색의 계열이 다른 것이 핵심이라 파란 계열로 바꾸지 마세요.

## B. Equipment/Lots (2026-09-14)

| 파일 | 바뀐 것 |
|---|---|
| `Management/EquipmentLotForm.cs` | Lot 클릭 시 카드를 비우지 않고 로딩 표시로 덮는다 · Remarks 두 줄 · 결정 값을 카드 제목에 적고 대상 줄이 반짝인다 |
| `Management/EquipmentLotForm.Designer.cs` | 의뢰서 카드 로딩 표시(`requestBusy`) 추가 · Remarks 영역 스크롤 |
| `Management/Services/TableMerge.cs` | 키 없는 표도 제자리에서 갱신한다(시편 표 깜빡임) · 자리 순서 갱신을 키를 선언하지 않은 표로만 좁혔다 |
| `Management/Services/EquipmentLotPresenter.cs` | 통신 모드를 Lot 의 장비에서 읽는다 · Start/End 의 장비 일치 조건 제거 · 결정 지속재가 작업의 타깃을 따른다 |
| `Hosting/ModernFormBase.cs` | 마지막 알림이 화면 하단에 한 줄로 남는다 |
| `Hosting/ModernFormBase.Messaging.cs` | 사용자가 누른 것을 거절할 때 토스트가 아니라 모달로 알린다 |

`Hosting/` 두 파일은 **베이스만 바뀌고 화면 소스는 한 줄도 바뀌지 않습니다.**

### 그 앞 회차에서 이미 받아 가신 것 (참고용으로 함께 둡니다)

| 파일 | 무엇 |
|---|---|
| `Management/Services/JobDecision.cs` | 결정이 장비 목록을 들고 다닌다(`EquipmentList`) |
| `Management/Contracts/EquipmentLotContracts.cs` | 게이트가 보는 상태 컬럼 목록 |
| `Management/Contracts/ServerFields.cs` | 장비 행의 `GOAL_PORT_NM` |

받은 파일이 맞는지 확인: `EquipmentLotForm.cs` 에 **`sameRequest` 가 2 번**, `RequestRemarkHeight` 값이 **64**.

`ServerFields.cs` 는 홈에서 상위 Common 으로 옮겼지만 회사 적용은 따로 정하기로 했으므로, 전달본에서는
그 이동에 딸린 `using` 한 줄을 빼 두었습니다.

## 화면 캡처

`shots/equipment-lots-2026-09-14*.png` — 전체 화면 · 작은 창 · 의뢰서 팝업.

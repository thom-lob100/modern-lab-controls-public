# 임시 전달 — Equipment/Lots 결정 패널 (2026-09-13, 갱신)

**앞선 전달분의 스플리터 구조 변경은 물렸습니다.** 스플리터를 아래 실행 줄까지 내리면 왼쪽 장비·Lot
열만 64px 더 내려와 열마다 바닥선이 어긋나서, 그 구조는 쓰지 않기로 했습니다. 이 파일에는 그 변경이
없습니다 — 폼 코드에 `OnActionSplitterMoved` 같은 것을 넣을 필요도 없습니다.

지금 들어 있는 것은 **결정 패널 두 가지**뿐이고, `Modern.Lab.Commons.dll`은 바뀌지 않습니다.

## 1. KPI 카드의 테두리·배경이 돌아왔습니다

카드 넷에 `Flat = true`가 들어가 테두리와 배경이 사라져 있었습니다(그 속성은 공용 카드 패널 위에 얹을
때 크롬을 없애라고 만든 것입니다). 네 줄을 지우고, 크롬이 돌아온 만큼 자리를 함께 줬습니다.

| 무엇 | 전 | 후 |
|---|---|---|
| KPI 카드 높이(`kpiEquipment`·`kpiPorts`·`kpiLot`·`kpiDurable`) | 79 | **96** |
| 둘째 줄 Y(`kpiLot`·`kpiDurable`) | 91 | **108** |
| KPI 그리드(`decisionGrid`) 높이 | 166 | **176** |
| 결정 카드(`decisionCard`) 높이 | 300 | **262** |

## 2. 창을 낮추면 지속재 목록이 줄고 결정 패널이 따라 올라옵니다

지속재 목록의 최소 높이가 바닥이라 세로가 줄어도 더는 줄지 못해 결정 패널이 밀려 잘렸습니다.

| 무엇 | 전 | 후 |
|---|---|---|
| 목록 최소 높이(`splitDurableDecision.Panel1MinSize`) | 138 | **80** |

## 3. 의뢰서 목록 깜빡임 — 폼 코드 (선택, 디자이너와 무관)

`EquipmentLotForm.cs`의 `BindRequestFields`가 조회할 때마다 필드 카드를 통째로 다시 만듭니다
(`DefineFields`가 값을 비우고 정의를 다시 세우고, 이어서 높이 재계산이 스플리터 배치를 건드려 아래
표까지 다시 그려집니다). 자동 갱신 주기마다 반복됩니다.

필드 하나를 클래스에 추가하고

```csharp
        private string requestFieldShape = string.Empty;
```

`BindRequestFields` 안의 아홉 줄을 이렇게 바꿉니다.

```csharp
            string shape = RequestFieldShape(fields);

            if (!string.Equals(shape, this.requestFieldShape, StringComparison.Ordinal))
            {
                this.fieldRequest.DefineFields(fields.ToArray());
                this.requestFieldShape = shape;
            }

            this.fieldRequest.SetRow(row);
            int fieldRows = (fields.Count + this.fieldRequest.Columns - 1) / this.fieldRequest.Columns;
            int fieldHeight = Math.Max(1, fieldRows) * 40 * this.DeviceDpi / 96;
            int remarkHeight = row == null ? 0 : 72 * this.DeviceDpi / 96;
            int headerHeight = fieldHeight + remarkHeight;

            if (this.tableRequestMaster.Height != headerHeight)
            {
                this.tableRequestMaster.RowStyles[0].Height = fieldHeight;
                this.tableRequestMaster.RowStyles[1].Height = remarkHeight;
                this.tableRequestMaster.Height = headerHeight;
                this.FitRequestHeader(headerHeight);
            }
```

그리고 `FitRequestHeader` 바로 앞에 헬퍼를 답니다(`using System.Text;`는 이미 있습니다).

```csharp
        private static string RequestFieldShape(List<ModernFieldDefinition> fields)
        {
            StringBuilder shape = new StringBuilder();

            for (int index = 0; index < fields.Count; index++)
            {
                shape.Append(fields[index].Member).Append(fields[index].IsLink ? "*" : string.Empty).Append('|');
            }

            return shape.ToString();
        }
```

## 파일

| 파일 | 무엇 |
|---|---|
| `Modern.Lab.Samples/Management/EquipmentLotForm.Designer.cs` | 1·2번 — 디자이너만 바꾸면 됩니다 |

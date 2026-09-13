# 임시 전달 — Equipment/Lots 하단 실행 줄 스플리터 (2026-09-13)

**이 저장소는 라이브러리 공개용이 아니라 단발 전달 통로입니다.** 정식 배포는 릴리즈 zip입니다.

이번 갱신분은 `EquipmentLotForm.Designer.cs` 한 파일이고, **폼 코드(`EquipmentLotForm.cs`)에 손으로
넣어야 하는 부분이 함께 있습니다**(아래 3절). 디자이너만 바꾸면 `splitActions`의 이벤트 핸들러가
없어 컴파일되지 않습니다. `Modern.Lab.Commons.dll`은 바뀌지 않습니다.

## 1. 무엇이 바뀌었나 — 스플리터가 아래 실행 줄까지 내려온다

의뢰서와 결정 패널을 가르던 세로 스플리터가 **위쪽에서 끝나고**, 아래 실행 줄은 별도 패널이라
Job 카드 폭을 코드가 흉내내고 있었습니다(`jobCard.Width = splitDurableDecision.Width`).

이제 실행 줄도 스플리터 하나(`splitActions`)의 두 칸입니다.

| 무엇 | 전 | 후 |
|---|---|---|
| `bottomPanel` 구성 | `actionCard`(Fill) + `gapActions`(Right 8) + `jobCard`(Right) | `splitActions`(Fill) 하나 |
| `actionCard` | `bottomPanel` 직속 | `splitActions.Panel1` |
| `jobCard` | `bottomPanel` 직속 · `Dock = Right` | `splitActions.Panel2` · `Dock = Fill` |
| 두 칸의 경계 | 코드가 폭만 맞춤 | **진짜 스플리터** — 끌 수 있고 위쪽 스플리터와 양방향으로 묶임 |

위를 끌면 아래가, 아래를 끌면 위가 따라옵니다. 결정 패널과 Job 카드는 항상 같은 폭입니다.

## 2. 함께 들어오는 것 (2026-09-13 앞선 전달분과 동일)

- KPI 카드 넷의 `Flat` 제거 — 테두리·배경 복구, 카드 79 → 96 · 둘째 줄 91 → 108 · 그리드 166 → 176 ·
  결정 카드 300 → 262.
- 지속재 목록 최소 높이 138 → 80 — 창을 낮추면 목록이 줄고 결정 패널이 따라 올라옵니다.

## 3. 폼 코드에 손으로 넣을 것 — `EquipmentLotForm.cs`

### 3-1. 필드 두 개 (`private bool fittingListPanels;` 아래)

```csharp
        private bool syncingActionColumns;
        private string requestFieldShape = string.Empty;
```

### 3-2. `SyncActionColumns` 첫 줄 교체 + 메서드 둘 추가

```csharp
        private void SyncActionColumns()
        {
            this.ApplyActionSplit(this.splitDurableDecision.Width);
            this.lblTarget.Width = Math.Max(0, this.ddbEquipment.Left - this.lblTarget.Left - this.actionCard.Padding.Left);
        }

        private void ApplyActionSplit(int jobWidth)
        {
            Modern.Lab.WinForms.Controls.Layout.ModernSplitContainer actions = this.splitActions;
            int room = actions.Width - actions.SplitterWidth;

            if (room < actions.Panel1MinSize + actions.Panel2MinSize)
            {
                return;
            }

            int job = Math.Max(actions.Panel2MinSize, Math.Min(jobWidth, room - actions.Panel1MinSize));
            int distance = room - job;

            if (actions.SplitterDistance == distance)
            {
                return;
            }

            this.syncingActionColumns = true;

            try
            {
                actions.SplitterDistance = distance;
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                this.syncingActionColumns = false;
            }
        }

        private void OnActionSplitterMoved(object sender, SplitterEventArgs e)
        {
            if (this.syncingActionColumns)
            {
                return;
            }

            Modern.Lab.WinForms.Controls.Layout.ModernSplitContainer actions = this.splitActions;
            Modern.Lab.WinForms.Controls.Layout.ModernSplitContainer right = this.splitRight;
            int job = actions.Width - actions.SplitterWidth - actions.SplitterDistance;
            int room = right.Width - right.SplitterWidth;

            if (room < right.Panel1MinSize + right.Panel2MinSize)
            {
                return;
            }

            int decision = Math.Max(right.Panel2MinSize, Math.Min(job, room - right.Panel1MinSize));
            int distance = room - decision;

            this.syncingActionColumns = true;

            try
            {
                if (right.SplitterDistance != distance)
                {
                    right.SplitterDistance = distance;
                }
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                this.syncingActionColumns = false;
            }

            this.SyncActionColumns();
        }
```

`OnActionSplitterMoved`는 디자이너가 `splitActions.SplitterMoved`에 거는 핸들러라 **이름이 정확해야**
합니다.

### 3-3. 의뢰서 목록 깜빡임 — `BindRequestFields` 안 (디자이너와 무관, 따로 적용 가능)

조회할 때마다 필드 카드를 통째로 다시 만들어(`DefineFields`가 값을 비우고 정의를 다시 세운다) 카드가
비워졌다 채워지고, 이어서 높이 재계산이 스플리터 배치를 건드려 아래 표까지 다시 그려졌습니다.
자동 갱신 주기마다 반복됩니다.

아홉 줄을 이렇게 바꿉니다.

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

그리고 `FitRequestHeader` 바로 앞에 비교용 헬퍼를 답니다(`using System.Text;`는 이미 있습니다).

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

**3-3 은 디자이너 없이 단독으로 적용됩니다** — 깜빡임만 먼저 보고 싶으시면 이것만 넣으셔도 됩니다.
그때 필요한 필드는 `requestFieldShape` 하나뿐입니다.

## 4. 확인해 주실 것

- 아래 실행 줄의 구분선이 위쪽 스플리터와 한 줄로 이어지는지, 끌었을 때 위아래가 같이 움직이는지.
- 의뢰서 목록의 깜빡임이 멎는지(자동 갱신을 켠 채로).

## 5. 검증 상태

홈에서 **솔루션 전체 빌드는 통과**했습니다. 화면 자체 검사는 이 구조 변경 뒤로 다시 돌리지 않았습니다 —
배치가 눈으로 확인될 항목이라 회사 확인을 먼저 받는 쪽을 택했습니다.

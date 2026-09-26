using System;
using System.Collections.Generic;
using System.Data;
using Modern.Lab.Controls.Wpf.Data;

namespace Modern.Lab.WinForms.Controls.Display
{
    /// <summary>
    /// 조회 결과의 컬럼을 그대로 정보 카드(<see cref="ModernFieldList"/>)에 싣는다 —
    /// 필드 목록을 손으로 적지 않는다. 캡션은 그리드 헤더와 **같은 용어사전**에서 나온다
    /// ("DURABLE_ID" → "Durable Id").
    ///
    /// ★ 필드를 늘리거나 줄이거나 순서를 바꾸는 일은 **쿼리**에서 한다.
    ///
    /// <code>
    /// FieldDefinitions.Of(durables)
    ///         .Hide("FOREIGN_YN")
    ///         .Span("DESCRIPTION", 5)
    ///         .Apply(this.fieldInfo);
    /// </code>
    ///
    /// 없는 컬럼 이름은 조용히 무시한다 — 쿼리가 바뀌어도 화면이 깨지지 않는다.
    /// <see cref="GridColumns"/>와 같은 어휘(Of · Only · Hide · Caption · Link · Badge · First)를 쓴다.
    /// </summary>
    public sealed class FieldDefinitions
    {
        private readonly List<ModernFieldDefinition> fields;

        private FieldDefinitions(DataTable table)
        {
            // 표시 대상 컬럼을 고르는 규칙은 Commons의 AutoColumns 한 곳에 있다 —
            // 카드가 스스로 정의할 때(ModernFieldList.SetRow)와 같은 결과여야 한다.
            string[] members = AutoColumns.MembersOf(table);
            this.fields = new List<ModernFieldDefinition>();

            foreach (string member in members)
            {
                this.fields.Add(new ModernFieldDefinition(member));
            }
        }

        public static FieldDefinitions Of(DataTable table)
        {
            return new FieldDefinitions(table);
        }

        /// <summary>행 하나만 들고 있을 때 — 그 행이 속한 표의 컬럼을 쓴다.</summary>
        public static FieldDefinitions Of(DataRow row)
        {
            return new FieldDefinitions(row == null ? null : row.Table);
        }

        /// <summary>적은 필드만 적은 순서대로 남긴다.</summary>
        public FieldDefinitions Only(params string[] members)
        {
            List<ModernFieldDefinition> kept = new List<ModernFieldDefinition>();

            foreach (string member in Names(members))
            {
                int index = this.IndexOf(member);

                if (index >= 0)
                {
                    kept.Add(this.fields[index]);
                }
            }

            if (kept.Count > 0)
            {
                this.fields.Clear();
                this.fields.AddRange(kept);
            }

            return this;
        }

        public FieldDefinitions Hide(params string[] members)
        {
            foreach (string member in Names(members))
            {
                int index = this.IndexOf(member);

                if (index >= 0)
                {
                    this.fields.RemoveAt(index);
                }
            }

            return this;
        }

        /// <summary>화면 문맥이 정하는 캡션 — 용어사전과 달라야 할 때만 쓴다.</summary>
        public FieldDefinitions Caption(string member, string caption)
        {
            return this.Replace(member, caption, -1, null);
        }

        /// <summary>값이 긴 필드를 여러 열로 넓힌다(Description 등).</summary>
        public FieldDefinitions Span(string member, int columnSpan)
        {
            return this.Replace(member, null, columnSpan, null);
        }

        /// <summary>값을 링크로 — 클릭하면 <see cref="ModernFieldList.FieldLinkClick"/>가 난다.</summary>
        public FieldDefinitions Link(params string[] members)
        {
            foreach (string member in Names(members))
            {
                this.Replace(member, null, -1, true);
            }

            return this;
        }

        /// <summary>
        /// 값을 상태 배지로 — 배경색은 값에서 유도된다(그리드 배지의 <c>BadgeAutoColor</c>와 같은 규칙).
        /// </summary>
        public FieldDefinitions Badge(params string[] members)
        {
            foreach (string member in Names(members))
            {
                int index = this.IndexOf(member);

                if (index >= 0)
                {
                    this.fields[index].IsBadge = true;
                }
            }

            return this;
        }

        /// <summary>
        /// 그 값일 때만 배지를 진한 오류 채움으로 바꾼다 — 해시 자동색은 값을 구분할 뿐
        /// 실패·거절이라는 의미를 주지 않으므로, 눈에 띄어야 하는 값을 여기에 적는다.
        /// </summary>
        public FieldDefinitions BadgeAccent(string member, string accentValues)
        {
            int index = this.IndexOf(member);

            if (index >= 0)
            {
                this.fields[index].IsBadge = true;
                this.fields[index].BadgeAccentValues = accentValues;
            }

            return this;
        }

        /// <summary>그 값일 때만 배지 테두리가 돈다 — 전송중·처리중 표시.</summary>
        public FieldDefinitions BadgeSpin(string member, string spinValues)
        {
            int index = this.IndexOf(member);

            if (index >= 0)
            {
                this.fields[index].IsBadge = true;
                this.fields[index].BadgeSpinValues = spinValues;
            }

            return this;
        }

        /// <summary>
        /// 값 표시 형식 — 그리드 <see cref="GridColumns"/>의 <c>Format</c>과 같은 문법이다
        /// ("N0", "yyyy-MM-dd"). 빈 값을 주면 <c>_TM</c> 기본 시각 서식까지 끄고 원래 값을 보여 준다.
        /// </summary>
        public FieldDefinitions Format(string member, string format)
        {
            int index = this.IndexOf(member);

            if (index >= 0)
            {
                this.fields[index].Format = format;
            }

            return this;
        }

        /// <summary>적은 순서대로 맨 앞으로 — 쿼리 순서가 카드 순서와 다를 때만 쓴다.</summary>
        public FieldDefinitions First(params string[] members)
        {
            int position = 0;

            foreach (string member in Names(members))
            {
                int index = this.IndexOf(member);

                if (index < 0)
                {
                    continue;
                }

                ModernFieldDefinition field = this.fields[index];
                this.fields.RemoveAt(index);
                this.fields.Insert(position, field);
                position++;
            }

            return this;
        }

        public ModernFieldDefinition[] ToArray()
        {
            return this.fields.ToArray();
        }

        /// <summary>
        /// 카드에 싣는다. <see cref="ModernFieldList.DefineFields"/>는 값을 비우므로,
        /// 값을 채우는 <see cref="ModernFieldList.SetRow"/>보다 **먼저** 부른다.
        /// 결과가 비어 있으면(조회 실패·null) 지금 필드를 그대로 둔다.
        /// </summary>
        public void Apply(ModernFieldList list)
        {
            if (list == null || this.fields.Count == 0)
            {
                return;
            }

            list.DefineFields(this.fields.ToArray());
        }

        // ModernFieldDefinition은 캡션·폭이 읽기 전용이다 — 같은 자리에 새로 만들어 바꾼다.
        private FieldDefinitions Replace(string member, string caption, int columnSpan, bool? isLink)
        {
            int index = this.IndexOf(member);

            if (index < 0)
            {
                return this;
            }

            ModernFieldDefinition current = this.fields[index];
            ModernFieldDefinition replaced = new ModernFieldDefinition(
                    caption ?? current.Caption,
                    current.Member,
                    columnSpan < 1 ? current.ColumnSpan : columnSpan);

            replaced.IsLink = isLink.HasValue ? isLink.Value : current.IsLink;

            // 새로 만들어 갈아 끼우므로 배지 설정도 함께 옮긴다 — 빠뜨리면
            // Badge(...) 뒤에 Caption(...)을 부르는 것만으로 배지가 조용히 사라진다.
            replaced.IsBadge = current.IsBadge;
            replaced.BadgeAccentValues = current.BadgeAccentValues;
            replaced.BadgeSpinValues = current.BadgeSpinValues;
            replaced.Format = current.Format;

            this.fields[index] = replaced;

            return this;
        }

        private int IndexOf(string member)
        {
            for (int index = 0; index < this.fields.Count; index++)
            {
                if (string.Equals(this.fields[index].Member, member, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }

        private static string[] Names(string[] names)
        {
            return names == null ? new string[0] : names;
        }
    }
}

# 의뢰서 조회 통일 — 2026-09-23

이번 작업에서 변경된 회사 적용 소스만 담았습니다.

## 적용

1. LogisticsRequestForm.cs와 LogisticsRequestForm.Designer.cs를 함께 교체합니다.
2. Dialogs/Requests/RequestInfoDialogForm.cs를 교체합니다.
3. deleted-files.txt의 RequestInfoData.cs를 회사 프로젝트와 디스크에서 삭제합니다. 클래식 csproj의 해당 Compile 항목도 삭제합니다.

Logistics 의뢰번호 링크는 폼베이스의 ShowRequestInfo → GetRequestInfo REQ_SERIAL_NO= → DataTable 표시 경로를 사용합니다.
회사 상위 Common의 기존 SendRequestText 연결을 사용하며, 폼에서 고정 샘플 데이터를 만들지 않습니다.
장비랏은 변경하지 않습니다. 기존 마스터·디테일 스냅숏 전달과 DataRow 입력 방식은 유지합니다.

홈 전용 HomeApi, 자체 검사, 프로젝트 전체 파일, DLL, 릴리즈 파일은 포함하지 않습니다.

원본 커밋: aa85a665302a378a08d1cd9fef38ed0de6c29218
검증: 빌드·공통 검사 통과, Logistics 33/0, Lot 117/0, Equipment/Lots 198/0.

## Hold 엔지니어 자동완성 수정 — 2026-09-23

Dialogs/Lots/LotHoldDialogForm.cs와 LotHoldDialogForm.Server.cs를 함께 교체합니다.
기존 Grid/Designer 및 상위 Common을 그대로 사용합니다. 회사에서 Server.cs 전문을 변경했다면
RequestEngineers(string keyword)의 Keyword 전달 부분을 기존 회사 조회에 반영합니다.

- 최초 전체 사용자 조회를 없애고 사번 또는 성명 입력 후 300ms에 입력값을 Keyword로 조회합니다.
- 응답은 USER_ID, USER_NM 컬럼을 포함해야 합니다. 회사 조회는 두 컬럼으로 검색해야 합니다.
- 후보 선택 시에만 EngrUserId를 채우고 성명을 오른쪽에 표시합니다. 다시 입력하면 선택을 해제합니다.
- 늦게 도착한 이전 검색 결과·실패를 무시하고, 빈 입력은 조회하지 않습니다.

원본 커밋: 28f33d5d7f6a411810d0cfd9a5a78851fd3dc15b
검증: 빌드·공통 검사 979/0, Lot 143/0, 신규 표시 64/0.
실제 회사 사용자 조회·DB 저장은 회사에서 확인합니다. 릴리즈·DLL 변경은 없습니다.

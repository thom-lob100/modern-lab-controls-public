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

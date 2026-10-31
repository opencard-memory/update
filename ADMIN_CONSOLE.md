# BSL 어드민 콘솔 (포트 9120)

서버 실행 시 자동으로 같이 시작됩니다: `http://<서버IP>:9120/`

* 최초 실행 때 `SaveBase/admin_config.json` 이 만들어지고 임의의 비밀번호가 콘솔에 출력됩니다. (파일에서 직접 바꿔도 됩니다. 변경 후 서버 재시작)
* `BindAddress`(기본 0.0.0.0), `Port`(기본 9120) 도 같은 파일에서 바꿀 수 있습니다.
* 외부 접속: 방화벽/클라우드 보안 그룹에서 TCP 9120 을 열어야 합니다.
* 로그인 5회 실패 시 해당 IP 10분 차단, 모든 작업은 `SaveBase/admin_audit.log` 에 기록됩니다.
* HTTP(평문)이므로 가능하면 허용 IP 제한 또는 HTTPS 리버스 프록시(nginx/caddy) 뒤에 두세요.

데이터 파일: `SaveBase/event_structure.json`(이벤트), `SaveBase/shop_catalog.json`(상점), `SaveBase/account_database.json`(유저).

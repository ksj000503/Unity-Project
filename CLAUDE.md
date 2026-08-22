# CLAUDE.md — 브로토토 모작 프로젝트 루트 가이드

> Claude Code 핸드오프 규격. 무기 시스템 현재 상태와 확정 규칙. 코드 위치: `Assets/Codes/Player/Weapon/`.

## 무기 시스템 아키텍처 (현행)

- **6슬롯**: `WeaponSlotManager`(Player 부착)가 `Awake`에서 앵커 6개를 반경 0.8 원주에 60° 균등 배치.
- **전략 패턴**: `Weapon`(탐지→쿨다운 루프) + 교체형 `IWeaponBehavior`(`MeleeSpinBehavior` / `RangedShootBehavior`). `Weapon.InitBehavior`가 `WeaponData.weaponType`으로 분기.
- **데이터**: `WeaponData`(ScriptableObject)는 **기준값 전용 공유 에셋**. 런타임에 절대 수정하지 않는다.

## 확정 규칙 — 키 입력 장착 & 중복 시 레벨업 (2026-08-22)

- **입력**: `WeaponInputHandler`(Player 부착, `Keyboard.current` 사용 — 신규 Input System). **키 1 = 근접**, **키 2 = 원거리** WeaponData 지급 요청 → `WeaponSlotManager.AddWeapon(data)`.
- **장착 규칙**: 같은 무기 미보유 시 **1번 슬롯부터 front-first** 로 빈 칸 채움. 6칸 초과 시 `false` 반환·무시(기존 규칙 유지).
- **중복 = 레벨업**: 동일 `WeaponData` 참조를 이미 장착 중이면 슬롯을 늘리지 않고 `Weapon.LevelUp()`. 슬롯이 가득 차 있어도 강화는 가능.
- **레벨 상태는 `Weapon` 인스턴스 런타임 값**(`level`, 기본 1). SO 오염 금지. 최종 수치는 계산 프로퍼티로 매 공격 시 즉시 반영:
  - `Weapon.Damage = round(data.damage × (1 + 0.2 × (level-1)))` — 레벨당 +20%, 상한 없음.
  - `Weapon.PierceCount = data.pierceCount + (level-1)/3` — 3레벨마다 관통 +1.
  - Behavior(근접·원거리)는 `data.damage`/`data.pierceCount`가 아닌 **`owner.Damage`/`owner.PierceCount`** 를 참조.

## 에디터 배선 (코드 반영 후 사람이 할 일)

1. Player 프리팹에 `WeaponInputHandler` 컴포넌트 추가(`WeaponSlotManager`와 같은 오브젝트).
2. `WeaponInputHandler`의 `meleeWeapon` = 기존 근접 `WeaponData.asset` 할당.
3. **원거리 `WeaponData.asset` 신규 생성**(Create ▸ Brotato/WeaponData): `weaponType=RangedShoot`, `weaponPrefab`=원거리 몸체, `projectilePrefab=Projectile_Basic`, `pierceCount=0` → `rangedWeapon`에 할당.
4. `PlayerMovement`의 임시 시작 무기 자동 지급(`Start`)은 제거됨. 이제 무기는 키 입력으로만 지급.

## 예외 처리 검증 체크리스트

| 상황 | 대응 | 반영 |
|------|------|------|
| 네트워크 끊김/지연 | 로컬 오프라인, 무관. 레벨은 지역 상태 | PASS |
| 필수값 누락/오형식 | WeaponData 미할당 키입력 무시+경고 / weaponPrefab null 시 장착 거부 / damage·pierce 계산에 `Mathf.Max`·`RoundToInt` 하한·정수화 / 키보드 디바이스 없으면 조용히 무시 | PASS |
| 비정상/권한 없는 접근 | 6슬롯 초과 `false` / 중복 판정은 동일 SO 참조로만 / `GetKeyDown` 대신 `wasPressedThisFrame`로 프레임당 1회 / `[RequireComponent(WeaponSlotManager)]` 강제 | PASS |

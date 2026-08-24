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

## 예외 처리 검증 체크리스트 (무기)

| 상황 | 대응 | 반영 |
|------|------|------|
| 네트워크 끊김/지연 | 로컬 오프라인, 무관. 레벨은 지역 상태 | PASS |
| 필수값 누락/오형식 | WeaponData 미할당 키입력 무시+경고 / weaponPrefab null 시 장착 거부 / damage·pierce 계산에 `Mathf.Max`·`RoundToInt` 하한·정수화 / 키보드 디바이스 없으면 조용히 무시 | PASS |
| 비정상/권한 없는 접근 | 6슬롯 초과 `false` / 중복 판정은 동일 SO 참조로만 / `GetKeyDown` 대신 `wasPressedThisFrame`로 프레임당 1회 / `[RequireComponent(WeaponSlotManager)]` 강제 | PASS |

## HP UI (2026-08-23)

- **체력 소스**: `Health`(`Assets/Codes/Common/`). `maxHp`/`currentHp`, `OnEnable`에서 풀피 리셋, `TakeDamage`→`Die`(풀 오브젝트면 `ObjectPoolManager.Return`으로 비활성, 아니면 Destroy).
- **변경 통지**: `Health.OnHealthChanged(current, max)` 이벤트 추가. `OnEnable`·`TakeDamage`에서 발행. **값이 바뀔 때만 갱신**(폴링 없음).
- **HP바**: `HealthBar`(Health와 같은 오브젝트에 부착). 월드스페이스 Canvas+Image 를 **런타임 자가 생성**(아트 에셋 불필요). 이벤트 구독으로 `Fill` RectTransform `localScale.x = current/max` 갱신. 캐릭터 **자식**이라 이동 추종 + 사망(풀 비활성) 시 함께 사라짐. `OnEnable` 재구독으로 풀 재사용 안전.
- **인스펙터**: `offset`(머리 위 오프셋, 로컬), `size`(로컬 가로/세로), `backColor`/`fillColor`, `sortingOrder`.

### 에디터 배선 (HP UI)
1. **Monster_Melee 프리팹**: 이미 `Health(maxHp=30)` 있음 → `HealthBar` 컴포넌트만 추가.
2. **Player**: `Health` 컴포넌트 **없음** → `Health`(maxHp 지정) + `HealthBar` 추가해야 바가 동작.

### 예외 처리 검증 체크리스트 (HP UI)

| 상황 | 대응 | 반영 |
|------|------|------|
| 네트워크 | 로컬, 무관 | PASS |
| 필수값 누락/오형식 | Health 없으면 경고+바 비활성 / `maxHp ≤ 0` 나눗셈 가드(0 표시) / 비율 `Clamp01` | PASS |
| 비정상/권한 없는 접근 | 풀 복귀·재사용 시 `OnDisable` 해제·`OnEnable` 재구독으로 상태 꼬임 방지 / 사망 중복 피격 무시(기존) | PASS |

## 돈 드랍 & 자석 획득 & 재화 저장 (2026-08-24)

코드 위치: `Assets/Codes/Currency/`. `Health`(Common)에 `OnDied` 이벤트 추가.

- **사망 훅**: `Health.OnDied` 이벤트(`Die()`에서 풀 복귀/파괴 직전 1회 발행). Player도 Health를 쓰지만 `CoinDropper`가 없어 드랍 안 함.
- **드랍**: `CoinDropper`(몬스터 부착, `[RequireComponent(Health)]`). `OnDied` 구독 → 사망 자리에 코인 스폰(`ObjectPoolManager` 풀링). **코인 가치 = `baseCoinValue × 현재 스테이지`**.
- **스테이지 값**: `StageManager` 싱글톤(`CurrentStage`, 기본 1). 씬에 없으면 코인 값은 스테이지 1로 계산. 이후 웨이브/스테이지 로직이 `SetStage`로 갱신하면 코인 값 자동 스케일.
- **자석 획득(Coin)**: `attractRange` 안이면 플레이어로 끌려가고(`FixedUpdate` 이동), `collectRange` 안이면 획득 → `CurrencyWallet.Add(value)` → 풀 복귀. 플레이어는 `"Player"` 태그로 탐색.
- **저장(CurrencyWallet)**: 플레이어 부착, 인메모리 골드 + `OnChanged` 이벤트(추후 재화 UI). `TrySpend`도 제공(상점 대비).

### 에디터 배선 (돈 시스템)
1. **Coin_Basic 프리팹 신규**: SpriteRenderer + `Coin` 컴포넌트. (콜라이더/리지드바디 불필요 — 거리 기반). 첫 스폰 시 풀 자동 등록.
2. **Monster_Melee 프리팹**: `CoinDropper` 추가 → `coinPrefab`에 Coin_Basic 할당, `baseCoinValue`/`dropCount` 설정.
3. **Player**: `CurrencyWallet` 컴포넌트 추가. `"Player"` 태그 확인.
4. (선택) 씬에 `StageManager` 오브젝트 배치 → 스테이지 진행 시 코인 값 스케일.

### 예외 처리 검증 체크리스트 (돈 시스템)

| 상황 | 대응 | 반영 |
|------|------|------|
| 네트워크 | 로컬, 무관 | PASS |
| 필수값 누락/오형식 | `coinPrefab` 미할당 드랍 스킵+경고 / Coin 컴포넌트 없으면 즉시 풀 반납 / 플레이어 Wallet 없으면 획득 무시+경고 / 값·스테이지 `Mathf.Max` 하한 | PASS |
| 비정상/권한 없는 접근 | 코인 중복 획득 `isCollected` 가드 / 풀 재사용 `OnEnable` 리셋 / `OnDied` 1회 발행 / `"Player"` 태그만 픽업 대상 | PASS |

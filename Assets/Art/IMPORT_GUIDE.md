# 에셋 임포트 가이드 (브로토토 모작)

플랫/벡터 풍 스프라이트 + UI 아이콘 세트. 모두 **투명 배경 PNG**, SVG 원본 동봉(무한 재조정용).

## 폴더 구성
```
assets/
├─ sprites/           # 게임 오브젝트 (256x256)
│  ├─ Player.png
│  ├─ Monster_Melee.png
│  ├─ Coin_Basic.png
│  ├─ Weapon_MeleeSpin.png
│  ├─ Weapon_RangedBasic.png
│  ├─ Projectile_Basic.png
│  └─ *.svg           # 각 스프라이트 벡터 원본
├─ icons/             # HUD/상점 UI (128x128)
│  ├─ icon_weapon_melee.png    # WeaponData.icon (근접)
│  ├─ icon_weapon_ranged.png   # WeaponData.icon (원거리)
│  ├─ icon_hp.png
│  ├─ icon_gold.png
│  ├─ icon_reroll.png
│  └─ *.svg
└─ _preview.png       # 전체 미리보기 시트
```

## 유니티 임포트 설정 (Inspector)

### 게임 스프라이트 (sprites/)
| 항목 | 값 |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single |
| Pixels Per Unit | **256** (256px 스프라이트 = 월드 1유닛). 크게/작게 = 값 조정 |
| Pivot | Center (Projectile_Basic만 필요 시 진행방향 기준으로 Custom) |
| Filter Mode | Bilinear |
| Compression | None 또는 High Quality (아트가 작아 용량 부담 없음) |

- 기존 플레이스홀더 **Square 스프라이트를 교체**: SpriteRenderer.Sprite를 각 오브젝트에 배정.
  - Player → `Player`, Monster_Melee 프리팹 → `Monster_Melee`, Coin_Basic → `Coin_Basic`.
- **콜라이더 반경은 스프라이트 교체 후 재확인**. 스핀 무기(spinRadius), 코인 자석(attractRange)은 시각 크기와 맞춰 미세조정.

### UI 아이콘 (icons/)
| 항목 | 값 |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Pixels Per Unit | 100 (UI라 무관, Image 컴포넌트로 크기 제어) |
| Filter Mode | Bilinear |

- `icon_weapon_melee` / `icon_weapon_ranged` → 각 **WeaponData.asset의 `icon` 필드**에 할당(상점 카드/HUD 슬롯 표시용).
- `icon_hp`, `icon_gold` → HUDManager 텍스트 옆 Image에.
- `icon_reroll` → ShopManager 리롤 버튼.

## 매핑 요약 (스펙 문서 대응)
- `Player` ↔ Player 오브젝트 (이동/무기 슬롯 6앵커의 중심)
- `Weapon_MeleeSpin` ↔ `Weapon_MeleeSpin` 프리팹의 비주얼 (360° 스핀 시 회전)
- `Weapon_RangedBasic` ↔ 원거리 무기 프리팹 (다음 세션 세팅 예정)
- `Projectile_Basic` ↔ `Projectile_Basic` 프리팹 (관통 발사체). Pivot을 좌/우 끝으로 두면 회전정렬 편함.
- `Coin_Basic` ↔ CoinDropper.coinPrefab

## 주의
- 스프라이트는 시각용일 뿐, `.prefab`/`.asset`(ScriptableObject)은 **유니티 에디터에서 직접 생성/배선**해야 정상 인식됩니다(스펙의 "남은 에디터 배선" 참고).
- 색/형태 수정이 필요하면 동봉된 `.svg`를 열어 편집 후 재출력하면 무손실입니다.

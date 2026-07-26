# Knight Skill Tree Design

## Muc tieu

Skill tree nen co 3 lop:

1. Core Knight: 4 active skill ro vai tro, moi skill co 2-3 upgrade node.
2. Passive Knight: tang attack, armor, crit, cooldown/value theo nhanh.
3. Element Gauntlet: mo element va reaction, ap vao hit cua Knight thay vi tao bo skill rieng.

He thong da co `CharacterSkillController` voi 4 active slot, nen 4 skill Knight nen la bo khung mac dinh:

## 4 active skill Knight

### 1. Shield Charge

Vai tro: mo giao tranh, bam muc tieu, ngat attack.

Nen dung asset/logic hien co: `Hero_ChargeStrike` + `HeroSkillChargeStrikeEffect`.

Thong so de can bang:

- Cooldown: 5-6s.
- Damage: 120-150% Attack.
- Co invincible ngan trong luc lao.
- Upgrade chinh:
  - Rank 1: unlock dash + hit cone.
  - Rank 2: +20% distance, -0.5s cooldown.
  - Rank 3: hit dau tien gay hit stun ngan.

Ly do: Knight can 1 nut vao combat nhanh, neu khong gameplay se chi di bo chem cham.

### 2. Ground Wave

Vai tro: ranged line, clear duong thang, kich hoat reaction an toan.

Nen dung asset/logic hien co: `Hero_GroundWave` + `HeroSkillLineDamageEffect`.

Thong so de can bang:

- Cooldown: 4-5s.
- Damage: 110-135% Attack.
- Width vua phai de can aim.
- Upgrade chinh:
  - Rank 1: unlock song kiem duong thang.
  - Rank 2: +25% width hoac length.
  - Rank 3: neu co gauntlet element, tang elemental status duration them 1s.

Ly do: day la skill tot nhat de gan Fire/Frost/Lightning/Poison vi no danh nhieu muc tieu nhung khong qua an toan nhu full screen.

### 3. Iron Guard

Vai tro: phong thu, song sot, tao nhip counter.

Nen dung asset/logic hien co: `Hero_IronGuard` + `HeroSkillStatBuffEffect`.

Thong so de can bang:

- Cooldown: 8-10s.
- Duration: 3-4s.
- Armor: +35-50%.
- Upgrade chinh:
  - Rank 1: unlock armor buff.
  - Rank 2: trong luc guard, nhan sat thuong giam them bang flat armor.
  - Rank 3: hit tiep theo sau guard them elementalPower hoac crit chance.

Ly do: Knight can ban sac "dung vung". Skill nay giup player co lua chon phong thu thay vi tat ca skill deu la damage.

### 4. Whirlwind

Vai tro: AoE DPS, don quai vay quanh, proc element nhieu lan.

Nen dung asset/logic hien co: `Hero_Whirlwind` + `HeroSkillWhirlwindEffect`.

Thong so de can bang:

- Cooldown: 9-12s.
- Duration: 1.8-2.4s.
- Damage moi tick: 35-50% Attack.
- Upgrade chinh:
  - Rank 1: unlock spin AoE.
  - Rank 2: +1 tick hoac +15% radius.
  - Rank 3: reaction damage tu Whirlwind +20%, nhung elemental status duration giam nhe neu can nerf.

Ly do: Whirlwind la skill tieu bieu de player thay gia tri cua gauntlet reaction, vi multi-hit tao cam giac build dang hoat dong.

## Skill khong nen nam trong 4 slot

`Shield Bash` nen la upgrade/counter node, khong nen la active rieng neu chi co 4 slot. Vi no trung vai tro voi Shield Charge: can chien, ngan, co stun. Dua no thanh node "Guard Counter" se tot hon:

- Sau khi dung Iron Guard, basic attack tiep theo thanh Shield Bash.
- Hoac Shield Charge neu trung muc tieu dang co Frost thi them Shield Bash shockwave.

## Element Gauntlet

Gauntlet khong nen tao 4 skill active moi. No nen la modifier layer:

- Chon/mang 1 element chinh.
- Skill Knight ap element vao hit.
- Doi element hoac co node dac biet de them secondary element.
- Reaction xay ra khi target dang co element A va bi danh element B.

### Element

Fire:

- Ban sac: damage over time, clear pack.
- Node nen co: +burn duration, +reaction damage.
- Hop voi: Whirlwind va Ground Wave.

Frost:

- Ban sac: control, setup burst.
- Node nen co: +status duration, giam toc hoac tao Brittle.
- Hop voi: Shield Charge va Ground Wave.

Lightning:

- Ban sac: burst, chain, interrupt.
- Node nen co: +reaction radius, neuroshock stun.
- Hop voi: Whirlwind.

Poison:

- Ban sac: damage dai, debuff armor.
- Node nen co: +DoT, +armor reduction.
- Hop voi: Iron Guard counter va Ground Wave.

### Reaction matrix

Fire + Frost = Shatter:

- Bonus single-target damage.
- Tot cho boss hoac elite.

Fire + Lightning = Overload:

- Bonus damage + splash AoE.
- Tot cho clear quai dong.

Frost + Lightning = Superconduct:

- Giam armor trong thoi gian ngan.
- Tot de mo combo truoc Whirlwind.

Fire + Poison = Burnout:

- Dot manh hon trong vai giay.
- Tot cho quai trau.

Lightning + Poison = Neuroshock:

- Bonus damage + hit stun ngan.
- Tot de cat attack cua elite.

Frost + Poison = Brittle Toxin:

- Giam armor vua phai lau hon Superconduct.
- Tot cho build cham, an toan.

## Layout de dung UI nhu anh

Nen chia thanh 3 cot lon:

- Technique: Shield Charge, Ground Wave, node damage/cooldown/crit.
- Defense: Iron Guard, Guard Counter, armor/max health.
- Gauntlet: Fire/Frost/Lightning/Poison va reaction node o giua cac element.

Quy tac unlock:

- Node active skill: cost 1, maxRank 1.
- Node upgrade skill: cost 1-2, maxRank 2-3, prerequisite active skill.
- Node reaction: cost 2, prerequisite ca 2 element node.
- Passive stat: maxRank 3-5, cost 1, dat xen giua cac skill de player co duong di.

## Thu tu build khuyen nghi

Early game:

1. Shield Charge.
2. Ground Wave.
3. Fire Gauntlet.
4. Overload neu da mo Lightning, hoac Shatter neu chon Frost.

Mid game:

1. Iron Guard.
2. Whirlwind.
3. Superconduct hoac Burnout.
4. Upgrade cooldown/damage cho skill chinh.

Late game:

1. Reaction node cap cao.
2. Whirlwind reaction bonus.
3. Guard Counter.
4. Passive crit/armor tuy build.

# Loot Knights - Project Reading Guide

Muc tieu cua file nay la giup ban mo project va doc script theo thu tu de hieu he thong, thay vi phai lan mo tung file mot cach ngau nhien.

## 1. Ban do thu muc script

`Assets/_Data/Core`
: Cac tien ich nen tang dung lai nhieu noi: `BaseMonoBehaviour`, singleton, pooling, SFX, VFX, Photon login/status.

`Assets/_Data/Gameplay`
: Luong choi cap cao. `GameFlow` dieu huong Main Menu, tao nhan vat, Lobby va load scene gameplay. `Dungeon` sinh phong, quan ly stage, phan thuong, chien thang/thua. `Faction` giup phan biet Hero va Enemy.

`Assets/_Data/Characters/Shared`
: Lop va component dung chung cho moi nhan vat: core character, movement, animation, damage, combat, skill runtime, stat, targeting, VFX, elemental, skill tree.

`Assets/_Data/Characters/Hero`
: Component rieng cho nguoi choi. Thu muc nay da duoc chia theo vai tro: `Core`, `Combat`, `Movement`, `Animation`, `Stats`, `Targeting`, `VFX`, va `Skill`.

`Assets/_Data/Characters/Enemy`
: Component rieng cho ke dich. Thu muc nay theo cung pattern voi Hero: `Core`, `Combat`, `Movement`, `Animation`, `Stats`, `Targeting`, `VFX`, `AI`, `Drop`, `Spawn`, va `Skill`.

`Assets/_Data/Economy`
: Du lieu va runtime cho currency, inventory, equipment, save/load item.

`Assets/_Data/UI`
: Cac panel va button UI theo man hinh: MainMenu, Lobby, Gameplay, CharacterMenu, Settings. UI thuong lang nghe event tu manager/runtime va render state.

`Assets/_Data/SkillTrees`
: Asset skill tree theo class/nhom. Script runtime cua skill tree nam trong `Characters/Shared/SkillTree`; UI hien thi nam trong `UI/CharacterMenu/Skill`.

`Assets/Resources`
: ScriptableObject/runtime assets ma code load bang `Resources`, nhu skill definitions, item database, stages, rooms, VFX/SFX definitions.

`Assets/Editor`
: Tool chi chay trong Unity Editor: capture preview, profile window, VFX installer, material converter.

`Assets/_ThirdParty`
: Asset/plugin ben ngoai. Khi doc logic game, nen bo qua thu muc nay truoc.

## 2. Nen doc theo thu tu nay

1. `Core/Base/BaseMonoBehaviour.cs` va `Core/Base/BaseSingleton.cs`
: Hieu pattern lifecycle cua project. Nhieu component override `LoadComponents`, `ResetValue`, `Start`, `Update`.

2. `Gameplay/GameFlow/GameFlowManager.cs`
: Hieu luong Main Menu -> Character Creation -> Lobby -> Stage Select -> GamePlay.

3. `Characters/Shared/Core/CharacterCtrl.cs`
: Hieu "hub" cua moi character. File nay giu reference den movement, stat, damage, combat, skill, targeting, VFX, level.

4. `Characters/Hero/Core/HeroCtrl.cs` va `Characters/Enemy/Core/EnemyCtrl.cs`
: Hieu cach Hero/Enemy ke thua tu `CharacterCtrl`, set faction, load controller rieng.

5. `Characters/Shared/Damage` va `Characters/Shared/Combat`
: Hieu damage flow: sender tao damage, receiver nhan damage, combat controller dieu phoi tan cong.

6. `Characters/Shared/Skill`
: Doc theo thu tu `Definitions` -> `Runtime` -> `Controllers` -> `Services` -> `Effects`. Day la xuong song cua skill/cast/cooldown/effect.

7. `Characters/Hero/Skill`
: Hieu cac effect cu the cua Hero nhu area damage, line damage, charge strike, whirlwind, projectile, elemental conduit.

8. `Characters/Shared/Elemental`
: Hieu trang thai nguyen to, shard/conduit, reaction, icon set.

9. `Gameplay/Dungeon/DungeonStageManager.cs` roi `DungeonGenerator.cs`
: Hieu gameplay scene bat dau stage, generate dungeon, grant reward, show victory/defeat.

10. `Economy/Inventory/Runtime/PlayerInventoryManager.cs`, `Economy/Equipment`, `Economy/Currency`
: Hieu item, tien, trang bi, save/load.

11. `UI`
: Doc sau khi da hieu data/runtime. UI trong project nay chu yeu la bridge giua button/panel va manager.

## 3. Convention dang duoc ap dung

- Class hien chua dung namespace, nen viec sap xep thu muc la cach chinh de tao kien truc de doc.
- Unity reference script bang GUID trong `.meta`; khi di chuyen script can di chuyen ca `.cs.meta`.
- Runtime data quan trong nam o ScriptableObject trong `Resources`, vi vay khi doc mot controller hay definition, hay kiem tra asset tuong ung trong Inspector.
- `Shared` nen chua logic tong quat, `Hero` va `Enemy` chi nen chua phan dac thu.
- UI khong nen giu qua nhieu game logic; nen goi manager/runtime va render state.

## 4. Luong gameplay rut gon

`GameFlowManager`
-> load/save `CreatedCharacterData`
-> vao `LobbyPanel` hoac `StageSelectPanel`
-> load scene `GamePlay`
-> `DungeonStageManager.StartCurrentStage`
-> `DungeonGenerator.Generate`
-> spawn Hero/Enemy
-> `CharacterCtrl` gom cac component con
-> combat/skill/damage/elements chay trong cac component shared va specializations
-> complete/fail stage
-> reward currency/experience va quay lai lobby/stage select.

## 5. Nguyen tac sap xep tiep theo

- Neu mot script ap dung cho ca Hero va Enemy, dua vao `Characters/Shared`.
- Neu script chi la bien the cua Hero, dua vao `Characters/Hero/<role>`.
- Neu script chi la bien the cua Enemy, dua vao `Characters/Enemy/<role>`.
- Neu script dieu phoi toan scene/game mode, dua vao `Gameplay`.
- Neu script la du lieu/tien/item/equipment/save inventory, dua vao `Economy`.
- Neu script chi render UI hoac bat su kien button, dua vao `UI`.
- Neu script chi dung trong Unity Editor, dua vao `Assets/Editor` hoac mot folder con ten `Editor`.

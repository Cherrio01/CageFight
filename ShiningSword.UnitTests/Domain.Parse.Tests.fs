module Domain.Parse.Tests
open Expecto

open Packrat
open Swensen.Unquote
open Expecto
open Domain
open Domain.Parser
open Domain.Random
open Domain.Random.Parser

let makeVerify() =
    let mutable counter = 0
    let addCounter label =
        counter <- counter + 1
        sprintf "%d) %s" counter label
    let verify = fun label condition -> testCase (addCounter label) <| fun _ -> Swensen.Unquote.Assertions.test condition
    let pverify = fun label condition -> ptestCase (addCounter label) <| fun _ -> Swensen.Unquote.Assertions.test condition
    verify, pverify
// Helper to give better error messages on parse fail
let parseFail ((args: ParseArgs), _) = Tests.failtestf "Could not parse %s" args.input

let parse (|Pattern|_|) input =
    match ParseArgs.Init input with
    | Pattern (v, End) -> v
    | v -> parseFail v

[<Tests>]
let UnitTests() = (testLabel "Unit tests" << testLabel "Domain") <| testList "Parse" [
    let verify, pverify = makeVerify()
    verify "Basic roll" <@ parse (|Roll|_|) "3d6" = RollSpec.create(3,6) @>
    verify "Basic roll" <@ parse (|Roll|_|) "3d-1" = RollSpec.create(3,6,-1) @>
    verify "Basic roll" <@ parse (|Roll|_|) "1d+1" = RollSpec.create(1,6,+1) @>
    verify "Basic roll" <@ parse (|Roll|_|) "4d" = RollSpec.create(4,6) @>
    verify "thr" <@ parse (|DamageOverall|_|) "thr" = (Thrust 0, None) @>
    verify "thr -1 imp" <@ parse (|DamageOverall|_|) "thr -1 imp" = (Thrust -1, Some Impaling) @>
    verify "thr-1" <@ parse (|DamageOverall|_|) "thr-1" = (Thrust -1, None) @>
    verify "sw+2 cut" <@ parse (|DamageOverall|_|) "sw+2 cut" = (Swing +2, Some Cutting) @>
    verify "4d-1 cr" <@ parse (|DamageOverall|_|) "4d-1 cr" = (Explicit (RollSpec.create(4,6,-1)), Some Crushing) @>
    ]

[<Tests>]
let AcceptanceTests() = testLabel "Acceptance tests" <| testList "Parsing" [
    let weaponStats (c: Creature) = c.WeaponSkill.Value, c.Damage_, defaultArg c.DamageType Other
    testList "Bob" [
        let verify, pverify = makeVerify()
        let creature = lazy(
            parse (|Creature|_|) "Bob the Barbarian: ST 17 DX 12 IQ 9 HT 11 HP 22 Speed 14 Weapon Master Skill 22 sw+2 cut"
            )
        verify "Is named Bob" <@ creature.Value.name = "Bob the Barbarian" @>
        verify "ST" <@ creature.Value.ST.Value = 17 @>
        verify "DX" <@ creature.Value.DX.Value = 12 @>
        verify "Skill, computed damage" <@ weaponStats creature.Value = (22, RollSpec.create(3,6,+7), Cutting)  @>
        ]
    testList "Peshkali" [
        let verify, pverify = makeVerify()
        let creature = lazy(
            parse (|Creature|_|) "Peshkali [Peshkalir]: ST 20 DX 12 IQ 14 HT 15 DR 4 HP 22 Speed 6 Weapon Master Skill 22 sw+1 cut Dodge 10 Parry 13 Extra Attack 5 Extra Parry 5"
            )
        verify "Name" <@ creature.Value.name = "Peshkali" @>
        verify "Name" <@ creature.Value.PluralName_ = "Peshkalir" @>
        verify "ST" <@ creature.Value.ST_ = 20 @>
        verify "DX" <@ creature.Value.DX_ = 12 @>
        verify "IQ" <@ creature.Value.IQ_ = 14 @>
        verify "HT" <@ creature.Value.HT_ = 15 @>
        verify "DR" <@ creature.Value.DR_ = 4 @>
        verify "HP" <@ creature.Value.HP_ = 22 @>
        verify "Speed" <@ creature.Value.Speed_ = 6.0 @>
        verify "WeaponMaster" <@ creature.Value.WeaponMaster = true @>
        verify "WeaponSkill" <@ creature.Value.WeaponSkill = Some 22 @>
        verify "Damage" <@ creature.Value.Damage_ = RollSpec.create(3,6,+9) @>
        verify "DamageType" <@ creature.Value.DamageType = Some Cutting @>
        verify "Dodge" <@ creature.Value.Dodge_ = 10 @>
        verify "Parry" <@ creature.Value.Parry = Some 13 @>
        verify "Block" <@ creature.Value.Block = None @>
        verify "ExtraAttack" <@ creature.Value.ExtraAttack_ = 5 @>
        verify "ExtraParry" <@ creature.Value.ExtraParry_ = 5 @>
        ]
    testList "Tiger" [
        let verify, pverify = makeVerify()
        let creature = lazy(
            parse (|Creature|_|) "Tiger: ST 13 DX 15 IQ 11 HT 12 Skill 16 2d-1 imp"
            )
        verify "Name" <@ creature.Value.name = "Tiger" @>
        verify "ST" <@ creature.Value.ST.Value = 13 @>
        verify "DX" <@ creature.Value.DX.Value = 15 @>
        verify "Skill, computed damage" <@ creature.Value.WeaponSkill = Some 16 && creature.Value.Damage_ = RollSpec.create(2,6,-1) && creature.Value.DamageType = Some Impaling  @>
        ]
    ]

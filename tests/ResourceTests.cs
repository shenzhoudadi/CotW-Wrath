using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using CallOfTheWild.Wrath.Prototype;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;

// Calls the shipped component through its real game runtime context. Only the
// fixture graph, current stat value and fact-active flag are seeded by reflection.
// This does not exercise Unity, EventBus dispatch, equipment/stat recalculation,
// rest, resource consumption, save/reload, or the in-game removal/respec pipeline.
internal static class ResourceTests
{
    private static string managedPath;
    private static string modPath;
    private static int scenarios;
    private static int assertions;

    private static int Main(string[] args)
    {
        if (args.Length < 2 || args.Length > 3)
        {
            Console.Error.WriteLine("Expected game Managed directory, prototype DLL path, and optional result JSON path.");
            return 2;
        }
        managedPath = Path.GetFullPath(args[0]);
        modPath = Path.GetFullPath(args[1]);
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try
        {
            Run();
            if (args.Length == 3)
                File.WriteAllText(args[2], "{\"scenariosPassed\":" + scenarios +
                    ",\"assertionsPassed\":" + assertions +
                    ",\"scope\":\"Production resource callback with real game managed types and manually seeded state; no Unity/game/EventBus/rest/save/respec test\"}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        var filename = new AssemblyName(args.Name).Name + ".dll";
        foreach (var directory in new[] { Path.GetDirectoryName(modPath), managedPath })
        {
            var path = Path.Combine(directory, filename);
            if (File.Exists(path)) return Assembly.LoadFrom(path);
        }
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run()
    {
        // Ensure that all following calls execute the supplied production DLL.
        Assert(Path.GetFullPath(typeof(ExtendedBaneComponent).Assembly.Location) == modPath,
            "Loaded the requested production assembly");
        var fixture = new Fixture();
        var target = fixture.Target;

        Scenario(() =>
        {
            fixture.SetWisdom(18);
            Assert(fixture.Wisdom.Bonus == 4, "Game attribute getter supplies the Wisdom bonus");
            Assert(fixture.Query(target, 0) == 4, "Positive Wisdom adds rounds");
            Assert(fixture.Query(target, 7) == 11, "Existing bonuses are preserved and added to");
            Assert(fixture.Query(target, -2) == 2, "Existing penalties are not overwritten");
        });

        Scenario(() =>
        {
            fixture.SetWisdom(10);
            Assert(fixture.Wisdom.Bonus == 0, "Neutral Wisdom bonus");
            Assert(fixture.Query(target, 7) == 7, "Neutral Wisdom leaves prior bonuses unchanged");
            fixture.SetWisdom(8);
            Assert(fixture.Wisdom.Bonus == -1, "Negative Wisdom bonus");
            Assert(fixture.Query(target, 7) == 7, "Wisdom penalty never removes rounds");
            fixture.SetWisdom(1);
            Assert(fixture.Query(target, -2) == -2, "Large Wisdom penalty leaves prior values unchanged");
        });

        Scenario(() =>
        {
            fixture.SetWisdom(18);
            Assert(fixture.Query(target, 0) == 4, "First query reads current Wisdom");
            fixture.SetWisdom(22);
            Assert(fixture.Query(target, 0) == 6, "Same component reads increased Wisdom on the next query");
            fixture.SetWisdom(12);
            Assert(fixture.Query(target, 0) == 1, "Same component reads reduced Wisdom on the next query");
            fixture.SetWisdom(11);
            Assert(fixture.Query(target, 0) == 0, "Odd Wisdom score follows the game's bonus rounding");
        });

        Scenario(() =>
        {
            fixture.SetWisdom(18);
            fixture.SetActive(false);
            Assert(!fixture.Fact.Active, "Fixture represents an inactive fact");
            Assert(fixture.Query(target, 7) == 7, "Inactive fact contributes no bonus");
            fixture.SetActive(true);
            Assert(fixture.Query(target, 7) == 11, "Reactivated fixture contributes again");
        });

        Scenario(() =>
        {
            // A removed fact is inactive and no longer has its owner manager.
            // Seed that state directly; this is NOT a test of the removal API.
            fixture.SetActive(false);
            SetField(fixture.Fact, "<Manager>k__BackingField", null);
            Assert(fixture.Fact.Owner == null, "Fixture represents an ownerless inactive fact");
            Assert(fixture.Query(target, 7) == 7, "Inactive detached state is harmless without an owner");
            SetField(fixture.Fact, "<Manager>k__BackingField", fixture.Manager);
            fixture.SetActive(true);
        });

        Scenario(() =>
        {
            var other = new BlueprintAbilityResource
            {
                name = "UnrelatedResource",
                AssetGuid = BlueprintGuid.Parse("0fa91e086c1543b99a6e5fbecc227a53")
            };
            Assert(fixture.Query(other, 7) == 7, "Other resource gains no rounds");
            Assert(fixture.Query(null, 7) == 7, "Null incoming resource is ignored");
            Assert(fixture.Query(target, 7) == 11, "Target resource still gains rounds after ignored queries");
        });

        Scenario(() =>
        {
            var reference = fixture.Component.Resource;
            fixture.Component.Resource = null;
            Assert(fixture.Query(target, 7) == 7, "Missing configured resource is ignored");
            fixture.Component.Resource = reference;
            Assert(fixture.Query(target, 7) == 11, "Restored resource reference resolves correctly");
        });

        Scenario(() =>
        {
            Assert(fixture.Query(target, 7) == 11, "First independent maximum query");
            Assert(fixture.Query(target, 7) == 11, "Repeated maximum query does not retain prior contributions");
            Assert(fixture.Wisdom.ModifiedValue == 18, "Query does not modify the owner's attribute");
            Assert(fixture.Fact.Active, "Query does not change fact activation");
        });

        Console.WriteLine($"PASS: {scenarios} managed resource callback scenarios, {assertions} assertions; production component and real game types, manually seeded states. No game/EventBus/rest/save/respec test.");
    }

    private sealed class Fixture
    {
        public readonly UnitFact Fact;
        public readonly EntityFactsManager Manager;
        public readonly ModifiableValueAttributeStat Wisdom;
        public readonly BlueprintAbilityResource Target;
        public readonly ExtendedBaneComponent Component;
        private readonly EntityFactComponent runtime;

        public Fixture()
        {
            // Constructors initialize world systems. Allocate only the minimum
            // real managed objects needed by the production owner/stat getters.
            var unit = Empty<UnitEntityData>();
            var descriptor = Empty<UnitDescriptor>();
            var stats = Empty<CharacterStats>();
            Wisdom = Empty<ModifiableValueAttributeStat>();
            SetField(unit, "<Descriptor>k__BackingField", descriptor);
            SetField(descriptor, "<Unit>k__BackingField", unit);
            SetField(descriptor, "Stats", stats);
            SetField(stats, "Wisdom", Wisdom);
            Manager = new EntityFactsManager(unit);
            Fact = new UnitFact();
            SetField(Fact, "<Manager>k__BackingField", Manager);
            SetActive(true);

            Target = new BlueprintAbilityResource
            {
                name = "ControlledBaneResource",
                AssetGuid = BlueprintGuid.Parse("c66d8bea989941fe86c010919834930a")
            };
            var reference = BlueprintReferenceBase.CreateTyped<BlueprintAbilityResourceReference>(Target);
            typeof(BlueprintReferenceBase).GetProperty("Cached", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(reference, Target, null);
            Component = new ExtendedBaneComponent { name = "$ExtendedBaneResourceTest", Resource = reference };
            runtime = Component.CreateRuntimeFactComponent();
            runtime.Setup(Component);
            runtime.RestoreOwnerLink(Fact);
        }

        public void SetWisdom(int value) => SetField(Wisdom, "<ModifiedValue>k__BackingField", value);
        public void SetActive(bool active) => SetField(Fact, "<IsActive>k__BackingField", active);

        public int Query(BlueprintAbilityResource resource, int initialBonus)
        {
            using (runtime.RequestEventContext())
                ((IResourceAmountBonusHandler)Component).CalculateMaxResourceAmount(resource, ref initialBonus);
            return initialBonus;
        }
    }

    private static T Empty<T>() where T : class => (T)FormatterServices.GetUninitializedObject(typeof(T));

    private static void SetField(object instance, string name, object value)
    {
        for (var type = instance.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field == null) continue;
            field.SetValue(instance, value);
            return;
        }
        throw new MissingFieldException(instance.GetType().FullName, name);
    }

    private static void Scenario(Action test)
    {
        test();
        scenarios++;
    }

    private static void Assert(bool passed, string message)
    {
        if (!passed) throw new Exception("FAILED: " + message);
        assertions++;
    }
}

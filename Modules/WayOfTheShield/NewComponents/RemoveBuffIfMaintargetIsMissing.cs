using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;

namespace Way_of_the_shield.NewComponents
{
    [AllowedOn(typeof(BlueprintBuff), false)]
    [TypeId("ad6ac06205f14400989447c987cf3103")]
    public class RemoveBuffIfMaintargetIsMissing : UnitBuffComponentDelegate, IAreaHandler, IGlobalSubscriber, IUnitHandler, IUnitSpawnHandler
    {
        public bool RemoveOnTargetDeath = true;
        private UnitEntityData MainTarget => Context?.SourceAbilityContext?.ClickedTarget?.Unit ?? Context?.MainTarget?.Unit;
        private void CheckTarget()
        {
            if (MainTarget == null || Context?.MaybeCaster == null) Buff?.Remove();
        }
        public void OnAreaBeginUnloading() { }
        public void OnAreaDidLoad() { CheckTarget(); }
        public void HandleUnitSpawned(UnitEntityData entityData) { }
        public void HandleUnitDestroyed(UnitEntityData entityData)
        {
            if (entityData == MainTarget || entityData == Context?.MaybeCaster) Buff?.Remove();
            else CheckTarget();
        }
        public void HandleUnitDeath(UnitEntityData entityData)
        {
            if (RemoveOnTargetDeath && entityData == MainTarget) Buff?.Remove();
        }
    }
}
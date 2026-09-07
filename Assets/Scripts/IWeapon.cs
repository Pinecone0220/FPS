namespace FpsPractice.FPS
{
    public interface IWeapon
    {
        int CurrentAmmo { get; }
        bool ShouldShowAmmo { get; }
        bool IsReadyToFire { get; }
        float MovementSpeedModifier { get; }
        void BeginEquip();
    }
}

public static class NetConfig
{
    // How many simulation steps per second
    public const int TICK_RATE = 30;

    // Time per tick (derived, do not change)
    public const float TICK_DELTA = 1f / TICK_RATE;

    // Hard server-side movement limit
    public const float MAX_MOVE_SPEED = 8f;
}

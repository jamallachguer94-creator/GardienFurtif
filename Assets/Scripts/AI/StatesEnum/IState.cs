// Every state class (Patrol, Chase, ...) must follow this contract.
// This is used in the second FSM version (state classes).
public interface IState
{
    string Name { get; } // used in the Console logs
    void Enter();        // runs once when we enter the state
    void Tick();         // runs every frame while in the state
    void Exit();         // runs once when we leave the state
}
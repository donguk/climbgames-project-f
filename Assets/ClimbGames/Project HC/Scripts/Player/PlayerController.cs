using ClimbGames.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private NavMeshAgent player;
        [SerializeField] private float moveSpeed = 5f;

        private VariableJoystick joystick;

        async void Start()
        {
            Transform parent = UIManager.Instance.transform.FindChildByName("HUD");
            joystick = await AssetManager.InstantiateAsync<VariableJoystick>("PlayerJoystick", parent);
        }

        void Update()
        {
            if (joystick == null)
                return;

            Vector3 direction = new Vector3(joystick.Horizontal, 0, joystick.Vertical);

            if (direction.sqrMagnitude > 0.01f)
            {
                // 캐릭터 회전
                player.transform.rotation = Quaternion.LookRotation(direction);

                // NavMesh 위에서 부드럽게 이동
                player.Move(direction * moveSpeed * Time.deltaTime);
            }

            //if (Keyboard.current.IsPressed())
        }
    }
}
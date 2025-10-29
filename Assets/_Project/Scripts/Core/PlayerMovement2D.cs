// Assets/_Project/Scripts/Core/PlayerMovement2D.cs
using UnityEngine;

namespace Game.Core
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        public float speed = 6f;
        Rigidbody2D rb;
        Vector2 input;

        void Awake() => rb = GetComponent<Rigidbody2D>();
        void Update()
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
            input = input.normalized;
        }
        void FixedUpdate()
        {
            rb.MovePosition(rb.position + input * speed * Time.fixedDeltaTime);
        }
    }
}

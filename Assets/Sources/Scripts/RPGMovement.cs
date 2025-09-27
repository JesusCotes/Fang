using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RPGMovement : Singleton<RPGMovement>
{
    public Rigidbody2D rb;
    float speed;
    public float speedWalk = 0.75f;
    public float speedRun = 1.5f;
    bool running;
    public Animator anim;
    int runDirection;

    float horizontal;
    float vertical;


/*     protected override void Awake()
    {
        base.Awake();
    } */

    // Update is called once per frame
    void FixedUpdate()
    {
        if(Input.GetKey("z") || Input.GetButton("Fire1")) {
            horizontal = Input.GetAxis("Horizontal") / 1;
            vertical = Input.GetAxis("Vertical") / 1;
            speed = speedRun;
        } else {
            horizontal = Input.GetAxis("Horizontal") / 2;
            vertical = Input.GetAxis("Vertical") / 2;
            speed = speedWalk;
        }
        anim.SetFloat("Horizontal", horizontal);
        anim.SetFloat("Vertical", vertical);

        Debug.Log("Horizontal: " + horizontal + " Vertical: " + vertical + " Speed: " + speed);
        rb.linearVelocity = new Vector2(horizontal, vertical) * speed;
    }
}

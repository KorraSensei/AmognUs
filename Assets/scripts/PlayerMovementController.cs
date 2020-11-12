using Mirror;
using UnityEngine;
using Cinemachine;


public class PlayerMovementController : NetworkBehaviour
{
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private CharacterController controller = null;

    private Vector2 previousInput;
    Vector3 moveVector;
    public Camera camera;
    public GameObject corpse;
    private bool em = true;
 
    public GameObject TeleportGoal;

    private Controls controls;

    private Controls Controls 
    {
        get
        {
            if(controls != null) { return controls; }
            return controls = new Controls();
        }
    }

    public override void OnStartAuthority()
    {
        enabled = true;
        TeleportGoal = GameObject.Find("teleport");

        Controls.Player.Move.performed += ctx => SetMovement(ctx.ReadValue<Vector2>());
        Controls.Player.Move.canceled += ctx => ResetMovement();
    }

    [ClientCallback]

    private void OnEnable() => Controls.Enable();
    [ClientCallback]

    private void onDisable() => Controls.Disable();

    [ClientCallback]
    private void Update() => Move();

    [Client]
    private void SetMovement(Vector2 movement) => previousInput = movement;

    [Client]
    private void ResetMovement() => previousInput = Vector2.zero;

    [Client]
    private void Move()
    {
        
        Vector3 right = controller.transform.right;
        Vector3 forward = controller.transform.forward;
        right.y = 0f;
        forward.y = 0f;

        Vector3 movement = right.normalized * previousInput.x + forward.normalized * previousInput.y;

        controller.Move(movement * movementSpeed * Time.deltaTime);

                moveVector = Vector3.zero;

        //Check if cjharacter is grounded
        if (controller.isGrounded == false)
        {
            //Add our gravity Vecotr
            moveVector += Physics.gravity;
        }

        //Apply our move Vector , remeber to multiply by Time.delta
        controller.Move(moveVector * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.F)){
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast (ray, out hit, (float)2) && hit.transform.tag == "Player"){
                Vector3 p = hit.transform.position;
                //p.y = p.y - 0.12f;
                //hit.transform.position = p;
                Quaternion r = hit.transform.rotation;
                Destroy(hit.collider.gameObject);
                GameObject go = (GameObject)Instantiate(corpse, p, r);
            }

            if (Physics.Raycast (ray, out hit, (float)4) && hit.transform.tag == "Embutton"){
                if (em == true) {
                    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Corpse");
                    foreach(GameObject enemy in enemies)
                    GameObject.Destroy(enemy);
                   
                    /*
                    GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                    foreach(GameObject player in players){
                        player.transform.position = TeleportGoal.transform.position;
                        em = false;
                    }
                    
                    if (em == false) {
                        Debug.Log("is false");
                    }
                    */
                }
                
            }
        }          

    }
}

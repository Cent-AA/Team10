using UnityEngine;
using System.Collections;
public class CameraMoving : MonoBehaviour
{
    private Camera Cam;
    public Transform player1, player2;
	public float minSizeY = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int pAmount;


    
    void Start()
    {
    Cam= GetComponent<Camera>();
    }
    void SetCameraPos()
    {
        Vector3 middle = (player1.position + player2.position) * 0.5f;
            transform.position = new Vector3(
		middle.x,
		middle.y,
        Cam.transform.position.z
		);

    }
    void SetCameraSize() {
		//horizontal size is based on actual screen ratio
		float minSizeX = minSizeY * Screen.width / Screen.height;

		//multiplying by 0.5, because the ortographicSize is actually half the height
		float width = +3f + Mathf.Abs(player1.position.x - player2.position.x) * 0.5f;
		float height = +3f + Mathf.Abs(player1.position.y - player2.position.y) * 0.5f;

		float camSizeX = Mathf.Max(width, minSizeX);

        GetComponent<UnityEngine.Camera>().orthographicSize  =Mathf.Max(height,
             camSizeX * Screen.height / Screen.width, minSizeY);;
            
	}
    // Update is called once per frame
    void Update()
    {
       // GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (Registry.Players.Count == 0)
        {
            return;
        }
                    if (Registry.Players.Count == 1)
        {
            Transform p = Registry.Players[0];
            transform.position = new Vector3(
                p.position.x,
                p.position.y,
                transform.position.z
            );
            Cam.orthographicSize = minSizeY;
            return;
        }
        player1 = Registry.Players[0];
        player2 = Registry.Players[1];
            SetCameraPos();
            SetCameraSize();
    //SetCameraPos();
    //SetCameraSize();
   // }
    }
}

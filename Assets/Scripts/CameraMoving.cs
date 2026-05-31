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
     GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
    //Vector3 cameraPos = transform.position;
    }
    void SetCameraPos()
    {
        Vector3 middle = (player1.position + player2.position) * 0.5f;
        //Cam.transform.position = new Vector3(
            transform.position = new Vector3(
		middle.x,
		middle.y,
        Cam.transform.position.z
		);
        //Vector3 cameraPos2 = Cam.transform.position;
        //Debug.Log(cameraPos2);
    }
    void SetCameraSize() {
		//horizontal size is based on actual screen ratio
		float minSizeX = minSizeY * Screen.width / Screen.height;

		//multiplying by 0.5, because the ortographicSize is actually half the height
        //Debug.Log(player1.position.x);
        //Debug.Log(player1.name);
		float width = +3f + Mathf.Abs(player1.position.x - player2.position.x) * 0.5f;
		float height = +3f + Mathf.Abs(player1.position.y - player2.position.y) * 0.5f;
        //Debug.Log(player1.position);
        //Debug.Log(width);
        //Debug.Log(height);
		//computing the size
		float camSizeX = Mathf.Max(width, minSizeX);
		//camera.orthographicSize = Mathf.Max(height,
		//camSizeX * Screen.height / Screen.width, minSizeY);
        GetComponent<UnityEngine.Camera>().orthographicSize  =Mathf.Max(height,
             camSizeX * Screen.height / Screen.width, minSizeY);;
            
	}
    // Update is called once per frame
    void Update()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        //Debug.Log(players.Length);
        if (players.Length >= 2)
        {
            player1 = players[0].transform;
            player2 = players[1].transform;
            SetCameraPos();
            SetCameraSize();
        }
            if (player1 == null || player2 == null)
        {
            return; 
        }
    //SetCameraPos();
    //SetCameraSize();
   // }
    }
}

using UnityEngine;

public class PCLook : MonoBehaviour
{
    [Header("Look Settings")]
    public float lookSpeed = 3f;      // Hur snabbt du kollar runt
    public float maxLookAngle = 35f;  // Hur långt du kan vrida huvudet (i grader)

    private Vector3 startRotation;
    private float currentX = 0f;
    private float currentY = 0f;

    void Start()
    {
        // Spara kamerans ursprungliga vinkel mot skärmen
        startRotation = transform.localEulerAngles;
    }

    void Update()
    {
        // Om vi håller in HÖGER musknapp (1) kan vi kolla runt!
        if (Input.GetMouseButton(1))
        {
            currentX += Input.GetAxis("Mouse X") * lookSpeed;
            currentY -= Input.GetAxis("Mouse Y") * lookSpeed;

            // Spärra så att vi inte bryter nacken (t.ex. max 35 grader åt sidorna)
            currentX = Mathf.Clamp(currentX, -maxLookAngle, maxLookAngle);
            currentY = Mathf.Clamp(currentY, -maxLookAngle, maxLookAngle);

            // Applicera rotationen
            transform.localRotation = Quaternion.Euler(startRotation.x + currentY, startRotation.y + currentX, startRotation.z);
        }
        else
        {
            // När du släpper musknappen: glid mjukt och snyggt tillbaka till mitten!
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(startRotation), Time.deltaTime * 6f);

            // Nollställ musens inmatning så det inte "hoppar" nästa gång du högerklickar
            currentX = Mathf.Lerp(currentX, 0, Time.deltaTime * 6f);
            currentY = Mathf.Lerp(currentY, 0, Time.deltaTime * 6f);
        }
    }
}
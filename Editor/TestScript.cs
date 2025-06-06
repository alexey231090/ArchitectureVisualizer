using UnityEngine;

public class TestScript : MonoBehaviour
{
    [SerializeField]
    private Transform playerTransform;

    [SerializeField]
    private Rigidbody playerRigidbody;

    private void Start()
    {
        var audioSource = GetComponent<AudioSource>();
        var camera = FindObjectOfType<Camera>();
    }
} 
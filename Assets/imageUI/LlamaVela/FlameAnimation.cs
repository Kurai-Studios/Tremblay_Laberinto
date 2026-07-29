using UnityEngine;
using UnityEngine.UI;

public class FlameAnimation : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 10f;

    private Image image;
    private int currentFrame = 0;
    private float timer = 0f;

    void Start()
    {
        image = GetComponent<Image>();
        currentFrame = Random.Range(0, frames.Length); // empieza en frame aleatorio
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= 1f / fps)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            image.sprite = frames[currentFrame];
        }
    }
}
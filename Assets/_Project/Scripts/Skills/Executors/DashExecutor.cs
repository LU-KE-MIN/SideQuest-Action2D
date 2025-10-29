using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashExecutor : MonoBehaviour
{
    private Rigidbody2D rb2d;
    private Rigidbody rb3d;
    private Collider2D col2d;
    private Collider col3d;
    
    private bool isDashing;
    private bool isInvulnerable;
    private List<GameObject> afterImages = new();
    
    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        rb3d = GetComponent<Rigidbody>();
        col2d = GetComponent<Collider2D>();
        col3d = GetComponent<Collider>();
    }
    
    public void PerformDash(
        Vector3 direction, 
        float distance, 
        float duration, 
        float invulnerabilityTime,
        bool passThroughEnemies,
        GameObject trailPrefab,
        int afterImageCount)
    {
        if (isDashing) return;
        
        StartCoroutine(DashRoutine(
            direction, 
            distance, 
            duration, 
            invulnerabilityTime, 
            passThroughEnemies,
            trailPrefab,
            afterImageCount
        ));
    }
    
    private IEnumerator DashRoutine(
        Vector3 direction, 
        float distance, 
        float duration, 
        float invulnerabilityTime,
        bool passThroughEnemies,
        GameObject trailPrefab,
        int afterImageCount)
    {
        isDashing = true;
        isInvulnerable = true;
        
        // Store original layer
        int originalLayer = gameObject.layer;
        if (passThroughEnemies)
        {
            gameObject.layer = LayerMask.NameToLayer("Dash");
        }
        
        // Spawn trail
        GameObject trail = null;
        if (trailPrefab != null)
        {
            trail = Instantiate(trailPrefab, transform.position, Quaternion.identity);
            trail.transform.SetParent(transform);
        }
        
        // Calculate dash
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + direction * distance;
        float elapsed = 0f;
        
        // Create after images
        float imageInterval = duration / (afterImageCount + 1);
        float nextImageTime = imageInterval;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Use easing for smooth movement
            float easedT = EaseOutCubic(t);
            
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, easedT);
            
            if (rb2d != null)
            {
                rb2d.MovePosition(newPos);
            }
            else if (rb3d != null)
            {
                rb3d.MovePosition(newPos);
            }
            else
            {
                transform.position = newPos;
            }
            
            // Create after image
            if (elapsed >= nextImageTime && afterImageCount > 0)
            {
                CreateAfterImage();
                nextImageTime += imageInterval;
            }
            
            yield return null;
        }
        
        // Clean up trail
        if (trail != null)
        {
            trail.transform.SetParent(null);
            var ps = trail.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop();
                Destroy(trail, ps.main.duration);
            }
            else
            {
                Destroy(trail, 1f);
            }
        }
        
        // Restore layer
        gameObject.layer = originalLayer;
        isDashing = false;
        
        // Handle invulnerability
        if (invulnerabilityTime > duration)
        {
            yield return new WaitForSeconds(invulnerabilityTime - duration);
        }
        
        isInvulnerable = false;
    }
    
    private void CreateAfterImage()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;
        
        GameObject afterImage = new GameObject("AfterImage");
        afterImage.transform.position = transform.position;
        afterImage.transform.rotation = transform.rotation;
        afterImage.transform.localScale = transform.localScale;
        
        var imageRenderer = afterImage.AddComponent<SpriteRenderer>();
        imageRenderer.sprite = spriteRenderer.sprite;
        imageRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        
        afterImages.Add(afterImage);
        StartCoroutine(FadeAfterImage(imageRenderer));
    }
    
    private IEnumerator FadeAfterImage(SpriteRenderer renderer)
    {
        float fadeTime = 0.5f;
        float elapsed = 0f;
        Color startColor = renderer.color;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeTime);
            renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        
        Destroy(renderer.gameObject);
    }
    
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
    
    public bool IsDashing => isDashing;
    public bool IsInvulnerable => isInvulnerable;
}
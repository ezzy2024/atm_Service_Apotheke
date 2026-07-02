import { useState, useEffect } from 'react';

export function useMediaPermissions() {
  const [hasPermissions, setHasPermissions] = useState<boolean | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    async function checkPermissions() {
      try {
        // Specifically request audio and video
        const stream = await navigator.mediaDevices.getUserMedia({ 
          audio: true, 
          video: true 
        });
        
        // If we got here, permissions were granted.
        if (isMounted) {
          setHasPermissions(true);
          setError(null);
        }
        
        // Immediately stop the tracks since we only wanted to check permissions.
        // Jitsi will request its own stream later.
        stream.getTracks().forEach(track => track.stop());
      } catch (err: any) {
        if (isMounted) {
          setHasPermissions(false);
          if (err.name === 'NotAllowedError') {
            setError('Berechtigung für Kamera oder Mikrofon verweigert. Bitte erlauben Sie den Zugriff im Browser.');
          } else if (err.name === 'NotFoundError') {
            setError('Keine Kamera oder kein Mikrofon gefunden. Bitte schließen Sie ein Gerät an.');
          } else {
            setError('Hardware-Zugriff fehlgeschlagen: ' + err.message);
          }
        }
      }
    }

    checkPermissions();

    return () => {
      isMounted = false;
    };
  }, []);

  return { hasPermissions, error };
}

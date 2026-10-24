const fallback = 'http://localhost:5254';

function savedOrigin() {
  try {
    const saved = localStorage.getItem('aangan.apiOrigin');
    if (saved && /^https?:\/\//.test(saved)) {
      return saved.replace(/\/$/, '');
    }
  } catch {
    // Storage is unavailable while the module is first evaluated.
  }

  return fallback;
}

export const environment = {
  apiUrl: `${savedOrigin()}/api`,
  hubUrl: `${savedOrigin()}/hubs/floor`,
};

export function apiOrigin() {
  return environment.apiUrl.replace(/\/api$/, '');
}

export function setApiOrigin(origin: string) {
  const base = origin.trim().replace(/\/$/, '');
  if (!/^https?:\/\/.+/.test(base)) {
    return false;
  }

  localStorage.setItem('aangan.apiOrigin', base);
  environment.apiUrl = `${base}/api`;
  environment.hubUrl = `${base}/hubs/floor`;
  return true;
}

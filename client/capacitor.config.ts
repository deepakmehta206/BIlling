import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'local.aangan.desk',
  appName: 'Aangan',
  webDir: 'dist/client/browser',
  server: {
    androidScheme: 'http',
    cleartext: true,
  },
};

export default config;

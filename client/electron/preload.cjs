const { contextBridge } = require('electron');

contextBridge.exposeInMainWorld('aanganShell', 'desktop');

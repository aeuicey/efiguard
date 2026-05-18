const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
  // Window
  minimizeWindow: () => ipcRenderer.invoke('window-minimize'),
  maximizeWindow: () => ipcRenderer.invoke('window-maximize'),
  closeWindow: () => ipcRenderer.invoke('window-close'),

  // System
  getSystemStatus: () => ipcRenderer.invoke('get-system-status'),
  isAdmin: () => ipcRenderer.invoke('is-admin'),

  // Operations
  disableVbs: () => ipcRenderer.invoke('disable-vbs'),
  getEfiGuardBundled: () => ipcRenderer.invoke('get-efiguard-bundled'),
  installEfiGuardBundled: () => ipcRenderer.invoke('install-efiguard-bundled'),

  // Dialogs
  showConfirm: (options) => ipcRenderer.invoke('show-confirm', options),
  showMessage: (options) => ipcRenderer.invoke('show-message', options)
});

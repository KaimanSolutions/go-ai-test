<script>
  import { onMount } from 'svelte';
  import { fetchBrandingSettings, saveBrandingSettings } from '../lib/auth.js';

  let branding = {
    primaryColor: '',
    accentColor: '',
    backgroundColor: '',
    surfaceColor: '',
    cardColor: '',
    textColor: '',
    mutedTextColor: '',
    fontBody: '',
    fontHeading: ''
  };

  let error = '';

  async function loadBranding() {
    const response = await fetchBrandingSettings();
    if (response.error) {
      console.warn('Failed to load branding settings:', response.error);
      error = response.error;
      return;
    }
    branding = { ...response };
  }

  async function saveBranding() {
    const response = await saveBrandingSettings(branding);
    if (response.error) {
      console.warn('Failed to save branding settings:', response.error);
      error = response.error;
      return;
    }
    alert('Branding settings saved successfully!');
  }

  onMount(loadBranding);
</script>

<main class="p-4">
  <h1 class="text-xl font-bold">Branding Settings</h1>
  {#if error}
    <p class="text-red-500">{error}</p>
  {/if}
  <div class="grid grid-cols-1 gap-4 mt-4">
    <div>
      <label for="primaryColor">Primary Color</label>
      <input id="primaryColor" type="color" bind:value={branding.primaryColor} />
    </div>
    <div>
      <label for="accentColor">Accent Color</label>
      <input id="accentColor" type="color" bind:value={branding.accentColor} />
    </div>
    <div>
      <label for="backgroundColor">Background Color</label>
      <input id="backgroundColor" type="color" bind:value={branding.backgroundColor} />
    </div>
    <div>
      <label for="surfaceColor">Surface Color</label>
      <input id="surfaceColor" type="color" bind:value={branding.surfaceColor} />
    </div>
    <div>
      <label for="cardColor">Card Color</label>
      <input id="cardColor" type="color" bind:value={branding.cardColor} />
    </div>
    <div>
      <label for="textColor">Text Color</label>
      <input id="textColor" type="color" bind:value={branding.textColor} />
    </div>
    <div>
      <label for="mutedTextColor">Muted Text Color</label>
      <input id="mutedTextColor" type="color" bind:value={branding.mutedTextColor} />
    </div>
    <div>
      <label for="fontBody">Body Font</label>
      <input id="fontBody" type="text" bind:value={branding.fontBody} />
    </div>
    <div>
      <label for="fontHeading">Heading Font</label>
      <input id="fontHeading" type="text" bind:value={branding.fontHeading} />
    </div>
  </div>
  <button class="mt-4 p-2 bg-blue-500 text-white rounded" on:click={saveBranding}>Save Settings</button>
</main>

<style>
  label {
    display: block;
    margin-bottom: 0.5rem;
    font-weight: bold;
  }
  input {
    width: 100%;
    padding: 0.5rem;
    border: 1px solid #ccc;
    border-radius: 0.25rem;
  }
</style>
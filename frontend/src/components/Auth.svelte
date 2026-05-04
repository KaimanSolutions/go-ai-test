<script>
  import { createEventDispatcher } from 'svelte';
  const dispatch = createEventDispatcher();
  let username = $state('');
  let password = $state('');

  function submitLogin() {
    dispatch('login', { username, password });
  }

  function startMicrosoftSSO() {
    dispatch('sso', { provider: 'microsoft' });
  }

  function startGoogleSSO() {
    dispatch('sso', { provider: 'google' });
  }
</script>

<div class="space-y-6">
  <div class="rounded-3xl border border-slate-800 bg-slate-950/90 p-6 shadow-xl ring-1 ring-white/5">

    <div class="space-y-5">
      <div>
        <label for="username" class="block text-sm font-medium text-slate-300">Username</label>
        <input
          id="username"
          type="text"
          class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-slate-100 shadow-sm outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20"
          bind:value={username}
          placeholder="admin"
        />
      </div>

      <div>
        <label for="password" class="block text-sm font-medium text-slate-300">Password</label>
        <input
          id="password"
          type="password"
          class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-slate-100 shadow-sm outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20"
          bind:value={password}
          placeholder="Password123!"
        />
      </div>

      <button
        class="w-full rounded-2xl bg-sky-500 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:bg-sky-400"
        onclick={submitLogin}
      >
        Login
      </button>

      <div class="relative">
        <div class="absolute inset-0 flex items-center">
          <div class="w-full border-t border-slate-800"></div>
        </div>
        <div class="relative flex justify-center text-sm">
          <span class="bg-slate-950 px-2 text-slate-400">Or continue with</span>
        </div>
      </div>

      <div class="grid grid-cols-2 gap-3">
        <button
          type="button"
          class="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-sm font-semibold text-slate-200 transition hover:bg-slate-800"
          onclick={startMicrosoftSSO}
        >
          <svg class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor">
            <path d="M11.4 24H0V12.6h11.4V24zM24 24H12.6V12.6H24V24zM11.4 11.4H0V0h11.4v11.4zm12.6 0H12.6V0H24v11.4z"/>
          </svg>
          Microsoft
        </button>
        <button
          type="button"
          class="inline-flex items-center justify-center gap-2 rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-sm font-semibold text-slate-200 transition hover:bg-slate-800"
          onclick={startGoogleSSO}
        >
          <svg class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor">
            <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
            <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
            <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
            <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
          </svg>
          Google
        </button>
      </div>
    </div>
  </div>
</div>

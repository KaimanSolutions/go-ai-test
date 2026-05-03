<script>
  import { createEventDispatcher } from 'svelte';
  const dispatch = createEventDispatcher();
  let username = $state('');
  let password = $state('');

  function submitLogin() {
    dispatch('login', { username, password });
  }

  function startSSO() {
    dispatch('sso');
  }
</script>

<div class="space-y-6">
  <div class="rounded-3xl border border-slate-800 bg-slate-950/90 p-6 shadow-xl ring-1 ring-white/5">
    <div class="mb-6">
      
      <p class="mt-3 text-sm leading-6 text-slate-400">Use your admin credentials or SSO to continue to the mortgage builder dashboard.</p>
    </div>

    <div class="space-y-5">
      <div>
        <label for="username" class="block text-sm font-medium text-slate-300">Username</label>
        <input
          id="username"
          class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-slate-100 shadow-sm outline-none transition focus:border-sky-500 focus:ring-2 focus:ring-sky-500/20"
          bind:value={username}
          placeholder="user@email.com"
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

      <div class="flex flex-col gap-3 sm:flex-row">
        <button
          class="inline-flex justify-center rounded-2xl bg-sky-500 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 transition hover:bg-sky-400"
          onclick={submitLogin}
        >
          Login
        </button>
        <button
          type="button"
          class="inline-flex justify-center rounded-2xl border border-slate-800 bg-slate-900 px-5 py-3 text-sm font-semibold text-slate-200 transition hover:bg-slate-800"
          onclick={startSSO}
        >
          Sign in with SSO
        </button>
      </div>
    </div>
  </div>
</div>

<script>
  import { onMount } from 'svelte';
  import FormRenderer from './FormRenderer.svelte';
  import { fetchSchemas, fetchSchema, validateSubmission } from '../lib/auth.js';

  let schemas = $state([]);
  let selectedSchema = $state(null);
  let loading = $state(true);
  let loadingSchema = $state(false);
  let error = $state('');
  let validationResult = $state(null);
  let submitted = $state(false);

  onMount(async () => {
    const response = await fetchSchemas();
    if (response.error) {
      error = response.error;
    } else {
      schemas = response;
    }
    loading = false;
  });

  async function selectSchema(id) {
    error = '';
    validationResult = null;
    submitted = false;
    loadingSchema = true;
    const response = await fetchSchema(id);
    if (response.error) {
      error = response.error;
    } else {
      selectedSchema = response;
    }
    loadingSchema = false;
  }

  async function handleSubmit(values) {
    if (!selectedSchema) return;
    error = '';
    const result = await validateSubmission(selectedSchema, values);
    if (result.error) {
      error = result.error;
      return;
    }
    validationResult = result;
    if (result.isValid) submitted = true;
  }

  function backToList() {
    selectedSchema = null;
    validationResult = null;
    submitted = false;
    error = '';
  }

  function startAgain() {
    validationResult = null;
    submitted = false;
    error = '';
    // re-fetch to reset renderer
    const id = selectedSchema.id;
    selectedSchema = null;
    selectSchema(id);
  }
</script>

<div class="space-y-6">
  {#if !selectedSchema && !loadingSchema}
    <!-- Schema picker -->
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <h3 class="text-lg font-semibold text-white">Select a form</h3>
      <p class="mt-1 text-sm text-slate-400">Choose a form schema to fill out.</p>
    </div>

    {#if error}
      <div class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</div>
    {/if}

    {#if loading}
      <p class="text-sm text-slate-400">Loading forms…</p>
    {:else if schemas.length === 0}
      <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-10 text-center shadow-xl">
        <p class="text-sm text-slate-400">No form schemas found. Create one in the Form Builder first.</p>
      </div>
    {:else}
      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {#each schemas as schema}
          <button
            onclick={() => selectSchema(schema.id)}
            class="group rounded-3xl border border-slate-800 bg-slate-900/95 p-6 text-left shadow-xl transition hover:border-sky-500/50 hover:bg-slate-800/80 focus:outline-none focus:ring-2 focus:ring-sky-500/40"
          >
            <div class="flex items-start justify-between gap-3">
              <div class="min-w-0">
                <h4 class="truncate text-base font-semibold text-white group-hover:text-sky-300 transition">{schema.title}</h4>
                {#if schema.description}
                  <p class="mt-1.5 text-sm text-slate-400 line-clamp-2">{schema.description}</p>
                {/if}
              </div>
              <svg class="mt-0.5 h-5 w-5 shrink-0 text-slate-600 group-hover:text-sky-400 transition" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clip-rule="evenodd"/>
              </svg>
            </div>
            <div class="mt-4 flex items-center gap-2">
              <span class="rounded-full bg-slate-800 px-2.5 py-1 text-xs font-medium text-slate-400">
                {schema.steps?.length ?? 0} {(schema.steps?.length ?? 0) === 1 ? 'step' : 'steps'}
              </span>
              <span class="rounded-full bg-slate-800 px-2.5 py-1 text-xs font-medium text-slate-400">
                {schema.steps?.reduce((n, s) => n + (s.fields?.length ?? 0), 0) ?? 0} fields
              </span>
            </div>
          </button>
        {/each}
      </div>
    {/if}

  {:else if loadingSchema}
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-10 text-center shadow-xl">
      <p class="text-sm text-slate-400">Loading form…</p>
    </div>

  {:else if submitted}
    <!-- Success state -->
    <div class="rounded-3xl border border-green-500/30 bg-green-500/5 p-10 text-center shadow-xl">
      <div class="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-green-500/20">
        <svg class="h-7 w-7 text-green-400" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M16.704 4.153a.75.75 0 01.143 1.052l-8 10.5a.75.75 0 01-1.127.075l-4.5-4.5a.75.75 0 011.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 011.05-.143z" clip-rule="evenodd"/>
        </svg>
      </div>
      <h3 class="text-xl font-semibold text-white">Form submitted successfully</h3>
      <p class="mt-2 text-sm text-slate-400">Your response for <span class="text-white font-medium">{selectedSchema.title}</span> has been recorded.</p>
      <div class="mt-6 flex justify-center gap-3">
        <button
          onclick={startAgain}
          class="rounded-2xl border border-slate-700 px-5 py-2.5 text-sm font-semibold text-slate-300 hover:bg-slate-800 transition"
        >Fill out again</button>
        <button
          onclick={backToList}
          class="rounded-2xl bg-sky-500 px-5 py-2.5 text-sm font-semibold text-white shadow-lg shadow-sky-500/20 hover:bg-sky-400 transition"
        >Back to forms</button>
      </div>
    </div>

  {:else}
    <!-- Form renderer -->
    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <div class="flex items-center justify-between gap-4">
        <div>
          <button
            onclick={backToList}
            class="mb-3 flex items-center gap-1.5 text-xs font-medium text-slate-400 hover:text-white transition"
          >
            <svg class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clip-rule="evenodd"/>
            </svg>
            Back to forms
          </button>
          <h3 class="text-xl font-semibold text-white">{selectedSchema.title}</h3>
          {#if selectedSchema.description}
            <p class="mt-1 text-sm text-slate-400">{selectedSchema.description}</p>
          {/if}
        </div>
        <div class="shrink-0 rounded-2xl bg-slate-950/80 px-3 py-1.5 text-xs font-medium text-slate-400">
          {selectedSchema.steps?.length ?? 0} {(selectedSchema.steps?.length ?? 0) === 1 ? 'step' : 'steps'}
        </div>
      </div>
    </div>

    {#if error}
      <div class="rounded-2xl bg-red-500/10 px-4 py-3 text-sm font-medium text-red-400">{error}</div>
    {/if}

    <div class="rounded-3xl border border-slate-800 bg-slate-900/95 p-6 shadow-xl">
      <FormRenderer schema={selectedSchema} {validationResult} onsubmit={handleSubmit} />
    </div>
  {/if}
</div>

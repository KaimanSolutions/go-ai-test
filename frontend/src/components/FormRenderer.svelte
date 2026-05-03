<script>
  import { run } from 'svelte/legacy';

  import { createEventDispatcher } from 'svelte';
  import { isFieldVisible } from '../lib/formBuilder.js';
  /**
   * @typedef {Object} Props
   * @property {any} schema
   * @property {any} [validationResult]
   */

  /** @type {Props} */
  let { schema, validationResult = null } = $props();
  const dispatch = createEventDispatcher();

  let currentStep = $state(0);
  let values = $state({});

  run(() => {
    if (schema) {
      values = {};
      schema.steps.forEach(step => {
        step.fields.forEach(field => {
          values[field.name] = field.defaultValue ?? '';
        });
      });
    }
  });

  function nextStep() {
    if (currentStep < schema.steps.length - 1) {
      currentStep++;
    }
  }

  function prevStep() {
    if (currentStep > 0) {
      currentStep--;
    }
  }

  function submit() {
    dispatch('submit', values);
  }
</script>

{#if schema}
  <div class="space-y-6">
    <h3 class="text-xl font-semibold text-white">{schema.steps[currentStep].title}</h3>

    {#each schema.steps[currentStep].fields as field}
      {#if isFieldVisible(field, values)}
        <div class="rounded-3xl border border-slate-800 bg-slate-950 p-5 shadow-xl">
          <label for={"field-" + field.name} class="block text-sm font-medium text-white">{field.label}</label>
          {#if field.type === 'select'}
            <select
              id={"field-" + field.name}
              class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"
              bind:value={values[field.name]}
            >
              <option value="">Select</option>
              {#each field.options as option}
                <option value={option}>{option}</option>
              {/each}
            </select>
          {:else if field.type === 'checkbox'}
            <div class="mt-3 flex items-center gap-3">
              <input
                id={"field-" + field.name}
                type="checkbox"
                class="h-5 w-5 rounded border-slate-800 text-sky-600 focus:ring-sky-500 bg-slate-900"
                bind:checked={values[field.name]}
              />
              <label for={"field-" + field.name} class="text-sm text-white">{field.label}</label>
            </div>
          {:else if field.type === 'number'}
            <input
              id={"field-" + field.name}
              type="number"
              class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"
              value={values[field.name] || ''}
              oninput={(e) => values[field.name] = e.target.value}
            />
          {:else}
            <input
              id={"field-" + field.name}
              type={field.type}
              class="mt-2 w-full rounded-2xl border border-slate-800 bg-slate-900 px-4 py-3 text-white shadow-sm focus:border-sky-500 focus:outline-none focus:ring-2 focus:ring-sky-200"
              value={values[field.name] || ''}
              oninput={(e) => values[field.name] = e.target.value}
            />
          {/if}
        </div>
      {/if}
    {/each}

    <div class="flex justify-between">
      {#if currentStep > 0}
        <button class="rounded-2xl bg-slate-600 px-4 py-2 text-sm font-semibold text-white" onclick={prevStep}>Previous</button>
      {/if}
      {#if currentStep < schema.steps.length - 1}
        <button class="rounded-2xl bg-sky-500 px-4 py-2 text-sm font-semibold text-white" onclick={nextStep}>Next</button>
      {:else}
        <button class="rounded-2xl bg-green-500 px-4 py-2 text-sm font-semibold text-white" onclick={submit}>Submit</button>
      {/if}
    </div>

    {#if validationResult}
      <div class="rounded-3xl border border-red-500 bg-red-50 p-5">
        <h4 class="text-lg font-semibold text-red-800">Validation Errors</h4>
        <ul class="mt-2 list-disc list-inside text-red-700">
          {#each Object.entries(validationResult.errors) as [field, messages]}
            {#each messages as message}
              <li>{message}</li>
            {/each}
          {/each}
        </ul>
      </div>
    {/if}
  </div>
{/if}

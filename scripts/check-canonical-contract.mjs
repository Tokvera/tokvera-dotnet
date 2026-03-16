const DEFAULT_BASE_URL = "https://api.tokvera.org";

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

async function fetchSchema(url) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`schema request failed: ${response.status} ${url}`);
  }
  const payload = await response.json();
  if (!payload?.ok || !payload?.schema) {
    throw new Error(`invalid payload from ${url}`);
  }
  return payload.schema;
}

async function main() {
  const base = (process.env.TOKVERA_API_BASE_URL || DEFAULT_BASE_URL).replace(/\/$/, "");
  const v2 = await fetchSchema(`${base}/v1/schema/event-envelope-v2`);
  assert(v2.envelope_version === "v2", "expected v2 envelope");
  assert(v2.schema_version === "2026-04-01", "unexpected schema version");
  assert(Array.isArray(v2.required_top_level_fields), "missing required_top_level_fields");
  assert(v2.provider_contracts?.openai?.event_type === "openai.request", "openai contract mismatch");
  assert(v2.provider_contracts?.tokvera?.endpoints?.includes("manual.trace"), "tokvera manual.trace missing");
  console.log(`tokvera-dotnet canonical contract check passed against ${base}`);
}

main().catch((error) => {
  console.error(error instanceof Error ? error.message : error);
  process.exit(1);
});
